using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Area;
using ProjectT.Death;
using ProjectT.Dialogue;
using ProjectT.Player;
using ProjectT.Save;
using ProjectT.UI;

namespace ProjectT.Session
{
    // DeathSequence가 FindAnyObjectByType으로 찾으므로 Core 씬에 하나만 둔다.
    public class CoreEntry : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [SerializeField] DialogueService dialogueService;
        [SerializeField] RoomLoader roomLoader;
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

        internal InputLock InputLock => inputLock;
        internal DialogueService Dialogue => dialogueService;
        internal bool EnteringAfterWindowClose => enteringAfterWindowClose;
        internal bool IsMovingRoom => roomLoader != null && roomLoader.IsMovingRoom;

        void Awake()
        {
            if (inputLock == null)
                Debug.LogError("[CoreEntry] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (dialogueService == null)
                Debug.LogError("[CoreEntry] 'dialogueService' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (roomLoader == null)
                Debug.LogError("[CoreEntry] 'roomLoader' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (laris == null)
                Debug.LogError("[CoreEntry] 'laris' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            else
                larisStart = laris.transform.position;

            // 첫 yield 전까지(진입 잠금·리스폰 복원)가 Awake 안에서 돌아야 다른 컴포넌트의 첫 Update보다 앞선다.
            RestartFromSave(afterDeath: false);
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

        // 시작과 사망 리로드가 같은 길을 탄다.
        internal void RestartFromSave(bool afterDeath) => StartCoroutine(RunRestartFromSave(afterDeath));

        IEnumerator RunRestartFromSave(bool afterDeath)
        {
            if (roomLoader != null)
            {
                IEnumerator unload = roomLoader.Unload();
                while (unload.MoveNext())
                    yield return unload.Current;
            }

            // 예전에는 씬 리로드가 Core 오브젝트까지 새로 만들어 자동으로 초기화됐다.
            ResetCore();
            EnterScene(afterDeath);
            RestoreRespawn(SaveManager.Data);

            // 리스폰 위치는 세이브된 Room의 좌표라 지금 있는 Room이 아니라 세이브된 Room을 불러온다.
            if (roomLoader != null)
            {
                IEnumerator load = roomLoader.Load(RoomToLoad(SaveManager.Data));
                while (load.MoveNext())
                    yield return load.Current;
                roomLoader.Enter();
            }

            if (enteringAfterWindowClose)
                RunWindowCloseEntry().Forget();

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
        internal void EnterScene(bool afterDeath)
        {
            if (afterDeath)
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
            DeathSequence.EndEntry();
        }

        internal ScreenFader FindOrCreateFader() => ScreenFader.FindOrCreate(faderPrefab, this);
    }
}
