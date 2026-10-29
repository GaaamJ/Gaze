using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Dialogue;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.UI;
using Object = UnityEngine.Object;

namespace ProjectT.Death
{
    // 라리스가 죽은 뒤 다시 시작할 때까지의 사망 처리 전체를 맡음.
    // DeathZone, DeathInteractable이 Begin을 부름.
    public static class DeathSequence
    {
        const float DialogueStopTimeout = 2f;
        const float RoomMoveTimeout = 10f;

        // 사망 시작 ~ CoreEntry의 리스폰 진입이 끝날 때까지 참.
        // 그동안 새 사망과 외면 대사를 받지 않음.
        public static bool IsActive { get; private set; }

        // Edit Test 모드에서는 씬 로딩이 안 되므로 바꿔 끼움.
        internal static Action<CoreEntry> Restart = entry => entry.RestartFromSave(afterDeath: true);

        // 도메인 리로드를 끈 플레이 모드에서도 실행마다 사망 상태를 비움.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void ResetState()
        {
            IsActive = false;
        }

        // 사망 시작. 입력을 잠근 후 Run.
        // 노드 이름이 비었거나 이미 사망 중이면 무시.
        public static void Begin(string deathNodeId)
        {
            if (string.IsNullOrWhiteSpace(deathNodeId))
            {
                Debug.LogWarning("[DeathSequence] 사망 노드 이름이 비어 있어 사망시키지 않았다.");
                return;
            }

            if (IsActive)
                return;

            CoreEntry scene = Object.FindAnyObjectByType<CoreEntry>();
            if (scene == null)
            {
                Debug.LogError($"[DeathSequence] 씬에 CoreEntry가 없어 사망 '{deathNodeId}'를 처리하지 못했다.");
                return;
            }

            IsActive = true;
            InputLock inputLock = scene.InputLock;
            if (inputLock != null)
                inputLock.Lock(InputLockReason.Death);

            Debug.Log($"[DeathSequence] 사망 시작: {deathNodeId}", scene);
            // 기다리지 않고 띄움. await 없이 시작한 YarnTask의 예외는 Forget()이 로그로 남김.
            Run(scene, inputLock, deathNodeId).Forget();
        }

        // 진행 중 대사 취소 → Room 이동 대기 → 사망 기록 → 사망 대사 → 페이드 아웃 → 세이브에서 다시 시작.
        // 중간에 실패하면 Abort로 되돌림.
        static async YarnTask Run(CoreEntry scene, InputLock inputLock, string deathNodeId)
        {
            bool reloading = false;
            ScreenFader fader = null;
            try
            {
                DialogueService dialogue = scene.Dialogue;
                if (dialogue != null)
                {
                    dialogue.RequestCancel();
                    if (!await dialogue.WaitUntilIdle(DialogueStopTimeout))
                        Debug.LogWarning("[DeathSequence] 진행 중인 대사가 제때 끝나지 않았다. 사망 노드가 거부될 수 있다.");
                }

                await WaitForRoomMove(scene);

                SaveProgress(deathNodeId);

                if (dialogue != null)
                {
                    DialogueResult result = await dialogue.Play(deathNodeId);
                    if (result == DialogueResult.Rejected)
                        Debug.LogWarning($"[DeathSequence] 사망 노드 '{deathNodeId}'를 재생하지 못했다. 그대로 리로드한다.");
                }
                else
                {
                    Debug.LogWarning($"[DeathSequence] DialogueService가 없어 사망 노드 '{deathNodeId}' 없이 리로드한다.");
                }

                fader = scene != null ? scene.FindOrCreateFader() : null;
                if (fader != null)
                {
                    // FadeOut은 코루틴(IEnumerator)이라 await할 수 없음. MoveNext를 한 프레임에 한 번씩 직접 돌려 끝까지 기다림.
                    IEnumerator fadeOut = fader.FadeOut();
                    while (fadeOut.MoveNext())
                        await YarnTask.Yield();
                }

                reloading = true;
                Restart(scene);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                reloading = false;
            }
            finally
            {
                // 리로드 진입 시 잠금·페이드 정리는 CoreEntry의 사망 진입이 맡음.
                if (!reloading)
                    Abort(inputLock, fader);
            }
        }

        // 다시 시작에 진입을 실패하면 사망 상태, 입력 잠금, 검은 화면을 되돌림.
        static void Abort(InputLock inputLock, ScreenFader fader)
        {
            IsActive = false;
            if (inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            if (fader != null)
                fader.SetAlpha(0f);
        }

        // 사망 기록만 저장. 리스폰 위치는 조사할 때 이미 저장됨.
        // 사망 대사 도중 게임을 꺼도 시체가 남도록 대사보다 먼저 기록.
        static void SaveProgress(string deathNodeId)
        {
            DeathRecords.Add(deathNodeId);
        }

        // 문으로 Room을 옮기는 중(RoomSceneLoader.IsMovingRoom)에 죽으면 그 이동이 끝날 때까지 기다림.
        // RoomMoveTimeout이 지나면 그대로 진행함.
        static async YarnTask WaitForRoomMove(CoreEntry scene)
        {
            float deadline = Time.realtimeSinceStartup + RoomMoveTimeout;
            while (scene != null && scene.IsMovingRoom)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogWarning("[DeathSequence] Room 이동이 제때 끝나지 않았다. 그대로 진행한다.");
                    return;
                }
                await YarnTask.Yield();
            }
        }

        // CoreEntry가 리스폰 진입을 끝내면 부름. 사망 종료 지점.
        internal static void EndEntry()
        {
            IsActive = false;
        }
    }
}
