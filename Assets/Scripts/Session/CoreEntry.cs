using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Yarn.Unity;
using Gaze.Area;
using Gaze.Death;
using Gaze.Dialogue;
using Gaze.Player;
using Gaze.Save;
using Gaze.UI;

namespace Gaze.Session
{
    // Core 씬의 진입점. 시작과 사망 후 다시 시작을 같은 길로 처리함.
    // 흐름: Room 해제 → Core 상태 초기화 → 입력 잠금(사망·창 닫기 진입) → 리스폰 위치 복원 → 세이브된 Room 로드 → 페이드 인 → 잠금 해제.
    // 창 닫기 후 재시작이면 진입 잠금 중에 창 닫기 사망 대사를 재생함.
    // DeathSequence가 FindAnyObjectByType으로 찾으므로 Core 씬에 하나만 둬야 함.
    public class CoreEntry : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [SerializeField] DialogueService dialogueService;
        [SerializeField] RoomSceneLoader roomLoader;
        [Tooltip("세이브에 리스폰 위치가 있으면 시작·사망 리로드 때 이 라리스를 옮긴다.")]
        [SerializeField] PlayerMovement laris;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;
        [Tooltip("세이브에 기록된 Room이 없을 때 불러올 Room 씬 이름")]
        [SerializeField] string defaultRoom = "Prototype";

        // 에디트 모드 테스트에서는 DialogueRunner가 돌지 않아 바꿔 끼움.
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

            // 첫 yield 전까지(진입 잠금·리스폰 복원)가 Awake 안에서 돌아야 다른 컴포넌트의 첫 Update보다 앞섬.
            RestartFromSave(afterDeath: false);
        }

        void OnEnable()
        {
            if (dialogueService != null)
                dialogueService.QuitNormallyRequested += NormalQuit.Request;
        }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금과 사망 상태를 되돌림.
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

        // 시작 때 불러올 Room 씬 이름을 고름.
        // 세이브의 roomName이 비었거나 빌드 설정에 없으면 defaultRoom.
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

        // 세이브 기준으로 다시 시작. 시작과 사망 리로드가 같은 길을 탐.
        // Awake(afterDeath: false)와 DeathSequence(afterDeath: true)가 부름.
        internal void RestartFromSave(bool afterDeath) => StartCoroutine(RunRestartFromSave(afterDeath));

        // 현재 Room 해제 → ResetCore → EnterScene → 리스폰 위치 복원 → 세이브된 Room 로드·진입 → 창 닫기 사망 대사(해당 시) → RunEntry.
        IEnumerator RunRestartFromSave(bool afterDeath)
        {
            if (roomLoader != null)
            {
                IEnumerator unload = roomLoader.Unload();
                while (unload.MoveNext())
                    yield return unload.Current;
            }

            // Core 씬은 다시 불러오지 않으므로 Core 상태를 직접 초기화함.
            ResetCore();
            EnterScene(afterDeath);
            RestoreRespawn(SaveManager.Data);

            // 리스폰 위치는 세이브된 Room의 좌표라 지금 있는 Room이 아니라 세이브된 Room을 불러옴.
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

        // 입력 잠금, 진입 상태, Yarn 변수, 라리스 위치(Awake 때 위치)를 처음 상태로 되돌림.
        void ResetCore()
        {
            if (inputLock != null)
                inputLock.Clear();
            holdingLock = false;
            enteringAfterWindowClose = false;
            if (dialogueService != null)
                dialogueService.ResetVariables();
            if (laris != null)
                PlayerMovement.MoveTo(laris.GetComponent<Rigidbody2D>(), larisStart);
        }

        // 사망 후 재시작이거나 창 닫기 사망이 대기 중이면 진입 상태로 들어가 Death 잠금을 건 채 시작함.
        // 둘 다 아니면 아무것도 하지 않음(일반 시작).
        // 다른 컴포넌트의 Update보다 먼저 잠가야 진입 첫 프레임에 방향키가 먹지 않음.
        // RunRestartFromSave가 부름. internal인 건 에디트 모드 테스트가 직접 부르기 위함.
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

        // 창 닫기 후 재시작 때 사망 대사를 재생하고 진입 잠금을 풂.
        // 창 닫기 사망은 발동 때 기록·시체 파일을 이미 저장했으므로 여기서는 사망 대사만 재생함.
        // RunRestartFromSave가 기다리지 않고 띄움(.Forget()). 예외는 이 안에서 로그로 남김.
        internal async YarnTask RunWindowCloseEntry()
        {
            if (!enteringAfterWindowClose)
                return;

            try
            {
                // LineAdvancer는 자기 Start에서 runner 프레젠터 목록에 끼어드는데 Start 순서는 정해져 있지 않음.
                // 같은 프레임에 시작하면 Z 진행 입력이 이 대화에 연결되지 않음.
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

        // 창 닫기 진입을 끝냄. 건 Death 잠금이 있으면 풂.
        // 이미 끝났으면 무시.
        void FinishWindowCloseEntry()
        {
            if (!enteringAfterWindowClose)
                return;
            if (holdingLock && inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            holdingLock = false;
            enteringAfterWindowClose = false;
        }

        // 세이브에 리스폰 위치가 있으면 라리스를 그 위치로 옮기고 카메라 상태를 리셋함.
        // 잠금 해제·페이드 인보다 먼저여야 이동이 보이지 않음.
        // RunRestartFromSave가 부름. internal인 건 에디트 모드 테스트가 직접 부르기 위함.
        internal void RestoreRespawn(SaveData data)
        {
            if (laris == null || !RespawnPoint.TryGet(data, out Vector2 position))
                return;

            PlayerMovement.MoveTo(laris.GetComponent<Rigidbody2D>(), position);
            CinemachineCore.ResetCameraState();
            Debug.Log($"[CoreEntry] 리스폰 위치로 시작: {position}", this);
        }

        // 사망 후 재시작 진입 연출. 검은 화면 → 한 프레임 뒤 잠금 해제 → 페이드 인.
        // 사망 후 재시작이 아니면 바로 끝남.
        // RunRestartFromSave가 마지막에 돌림. internal인 건 에디트 모드 테스트가 직접 돌리기 위함.
        internal IEnumerator RunEntry()
        {
            if (!enteringAfterDeath)
                yield break;

            // 반복기 안에서는 yield를 catch로 감쌀 수 없어 finally로 정리만 보장함.
            // 예외는 Unity 코루틴 실행기가 로그로 남김.
            try
            {
                ScreenFader fader = FindOrCreateFader();
                if (fader != null)
                    fader.SetAlpha(1f);

                // 검은 화면과 Death 잠금을 한 프레임 유지한 뒤 풂.
                yield return null;

                // 잠금을 페이드 인보다 먼저 풀어 페이드 인 중에도 조작할 수 있음.
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
                // 페이드 인 중 새 사망이 시작됐을 수 있어, 이미 끝낸 진입은 다시 끝내지 않음.
                if (enteringAfterDeath)
                    FinishEntry();
            }
        }

        // 사망 후 재시작 진입을 끝냄. 건 Death 잠금을 풀고 DeathSequence의 사망 상태를 끝냄.
        void FinishEntry()
        {
            if (holdingLock && inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            holdingLock = false;
            enteringAfterDeath = false;
            DeathSequence.EndEntry();
        }

        // 씬의 ScreenFader를 찾고 없으면 faderPrefab으로 만듦. DeathSequence도 부름.
        internal ScreenFader FindOrCreateFader() => ScreenFader.FindOrCreate(faderPrefab, this);
    }
}
