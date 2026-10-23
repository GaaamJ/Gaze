using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using ProjectT.Area;
using ProjectT.Dialogue;
using ProjectT.Player;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Death
{
    // DeathSequence가 FindAnyObjectByType으로 찾으므로 Core 씬에 하나만 둔다.
    public class CoreEntry : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [SerializeField] DialogueService dialogueService;
        [Tooltip("세이브에 리스폰 위치가 있으면 시작·사망 리로드 때 이 라리스를 옮긴다.")]
        [SerializeField] PlayerMovement laris;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;
        [Tooltip("세이브에 기록된 Room이 없을 때 불러올 Room 씬 이름")]
        [SerializeField] string defaultRoom = "ParkingLot";

        // 에디트 모드 테스트에서는 DialogueRunner가 돌지 않아 바꿔 끼운다.
        internal static Func<DialogueService, string, YarnTask<DialogueResult>> PlayDialogue = (dialogue, node) => dialogue.Play(node);

        bool enteringAfterDeath;
        bool enteringAfterWindowClose;
        bool holdingLock;
        Vector3 larisStart;
        string roomName;
        Scene room;

        internal InputLock InputLock => inputLock;
        internal DialogueService Dialogue => dialogueService;
        internal bool EnteringAfterWindowClose => enteringAfterWindowClose;

        void Awake()
        {
            if (inputLock == null)
                Debug.LogError("[CoreEntry] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (dialogueService == null)
                Debug.LogError("[CoreEntry] 'dialogueService' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (laris == null)
                Debug.LogError("[CoreEntry] 'laris' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            else
                larisStart = laris.transform.position;

            EnterScene();
            RestoreRespawn(SaveManager.Data);
        }

        IEnumerator Start()
        {
            IEnumerator load = LoadRoom(RoomToLoad(SaveManager.Data));
            while (load.MoveNext())
                yield return load.Current;

            if (enteringAfterWindowClose)
                RunWindowCloseEntry().Forget();
        }

        void OnEnable()
        {
            if (dialogueService != null)
                dialogueService.QuitNormallyRequested += NormalQuit.Request;
        }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금과 사망 상태를 되돌린다.
        void OnDisable()
        {
            if (dialogueService != null)
                dialogueService.QuitNormallyRequested -= NormalQuit.Request;

            if (enteringAfterWindowClose)
                FinishWindowCloseEntry();
            if (!enteringAfterDeath)
                return;
            StopAllCoroutines();
            FinishEntry();
        }

        internal string RoomToLoad(SaveData data)
        {
            string saved = data != null ? data.roomName : null;
            if (string.IsNullOrWhiteSpace(saved))
                return defaultRoom;
            if (!Application.CanStreamedLevelBeLoaded(saved))
            {
                Debug.LogWarning($"[CoreEntry] 세이브의 Room '{saved}'가 빌드 설정에 없어 '{defaultRoom}'을 불러온다.", this);
                return defaultRoom;
            }
            return saved;
        }

        IEnumerator LoadRoom(string name)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[CoreEntry] Room 씬 '{name}'을 불러오지 못했다. 빌드 설정을 확인해야 한다.", this);
                yield break;
            }
            while (!load.isDone)
                yield return null;

            roomName = name;
            room = SceneManager.GetSceneByName(name);
            SceneManager.SetActiveScene(room);

            RoomEntry entry = FindRoomEntry(room);
            if (entry == null)
                Debug.LogWarning($"[CoreEntry] Room 씬 '{name}'에 RoomEntry가 없다.", this);
            else
                entry.Enter();
        }

        static RoomEntry FindRoomEntry(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RoomEntry entry = root.GetComponentInChildren<RoomEntry>(true);
                if (entry != null)
                    return entry;
            }
            return null;
        }

        internal void ReloadRoom() => StartCoroutine(RunReloadRoom());

        IEnumerator RunReloadRoom()
        {
            if (room.IsValid() && room.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(room);
                while (unload != null && !unload.isDone)
                    yield return null;
            }

            // 예전에는 씬 리로드가 Core 오브젝트까지 새로 만들어 자동으로 초기화됐다.
            ResetCore();
            EnterScene();
            RestoreRespawn(SaveManager.Data);

            IEnumerator load = LoadRoom(string.IsNullOrEmpty(roomName) ? RoomToLoad(SaveManager.Data) : roomName);
            while (load.MoveNext())
                yield return load.Current;

            IEnumerator entry = RunEntry();
            while (entry.MoveNext())
                yield return entry.Current;
        }

        void ResetCore()
        {
            if (inputLock != null)
                inputLock.Clear();
            holdingLock = false;
            enteringAfterWindowClose = false;
            if (dialogueService != null)
                dialogueService.ResetVariables();
            if (laris != null)
                Teleport.MoveTo(laris.GetComponent<Rigidbody2D>(), larisStart);
        }

        // 다른 컴포넌트의 Update보다 먼저 잠가야 진입 첫 프레임에 방향키가 먹지 않는다.
        internal void EnterScene()
        {
            if (DeathSequence.ConsumeReload())
                enteringAfterDeath = true;
            else if (GameSession.ConsumePendingDeath())
                enteringAfterWindowClose = true;
            else
                return;

            if (inputLock != null)
            {
                inputLock.Lock(InputLockReason.Death);
                holdingLock = true;
            }
        }

        // 창 닫기 사망은 발동 때 기록·시체 파일을 이미 저장했으므로 여기서는 사망 대사만 재생한다.
        internal async YarnTask RunWindowCloseEntry()
        {
            if (!enteringAfterWindowClose)
                return;

            try
            {
                // LineAdvancer는 자기 Start에서 runner 프레젠터 목록에 끼어드는데 Start 순서는 정해져 있지 않다. 같은 프레임에 시작하면 Z 진행 입력이 이 대화에 연결되지 않는다.
                await YarnTask.Yield();
                if (!enteringAfterWindowClose)
                    return;

                const string node = ExitRecorder.WindowCloseDeathNodeId;
                if (dialogueService == null)
                {
                    Debug.LogWarning($"[CoreEntry] DialogueService가 없어 창 닫기 사망 노드 '{node}' 없이 재개한다.", this);
                }
                else if (await PlayDialogue(dialogueService, node) == DialogueResult.Rejected)
                {
                    Debug.LogWarning($"[CoreEntry] 창 닫기 사망 노드 '{node}'를 재생하지 못했다. 그대로 재개한다.", this);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                FinishWindowCloseEntry();
            }
        }

        void FinishWindowCloseEntry()
        {
            if (!enteringAfterWindowClose)
                return;
            if (holdingLock && inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            holdingLock = false;
            enteringAfterWindowClose = false;
        }

        // 잠금 해제·페이드 인보다 먼저여야 이동이 보이지 않는다.
        internal void RestoreRespawn(SaveData data)
        {
            if (laris == null || !RespawnPoint.TryGet(data, out Vector2 position))
                return;

            Teleport.MoveTo(laris.GetComponent<Rigidbody2D>(), position);
            CinemachineCore.ResetCameraState();
            Debug.Log($"[CoreEntry] 리스폰 위치로 시작: {position}", this);
        }

        internal IEnumerator RunEntry()
        {
            if (!enteringAfterDeath)
                yield break;

            // 반복기 안에서는 yield를 catch로 감쌀 수 없어 finally로 정리만 보장하고, 예외는 Unity 코루틴 실행기가 로그로 남긴다.
            try
            {
                ScreenFader fader = FindOrCreateFader();
                if (fader != null)
                    fader.SetAlpha(1f);

                // 현재 동작: 검은 화면과 Death 잠금을 한 프레임 유지한 뒤 푼다.
                yield return null;

                // 현재 동작: 잠금을 페이드 인보다 먼저 풀어 페이드 인 중에도 조작할 수 있다.
                FinishEntry();

                if (fader != null)
                {
                    IEnumerator fadeIn = fader.FadeIn();
                    while (fadeIn.MoveNext())
                        yield return fadeIn.Current;
                }
            }
            finally
            {
                // 페이드 인 중 새 사망이 시작됐을 수 있어, 이미 끝낸 진입은 다시 끝내지 않는다.
                if (enteringAfterDeath)
                    FinishEntry();
            }
        }

        void FinishEntry()
        {
            if (holdingLock && inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            holdingLock = false;
            enteringAfterDeath = false;
            DeathSequence.CompleteReload();
        }

        internal ScreenFader FindOrCreateFader() => ScreenFader.FindOrCreate(faderPrefab, this);
    }
}
