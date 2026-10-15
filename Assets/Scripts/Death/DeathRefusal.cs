using System;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Dialogue;
using ProjectT.Player;
using ProjectT.Session;
using Object = UnityEngine.Object;

namespace ProjectT.Death
{
    public static class DeathRefusal
    {
        // 사망 리로드로 씬이 바뀌면 진입점이 파괴되어 가드가 저절로 풀린다.
        static DeathSceneEntry activeScene;

        public static bool IsActive => activeScene != null;

        // 에디트 모드 테스트에서는 DialogueRunner가 돌지 않아 바꿔 끼운다.
        internal static Func<DialogueService, string, YarnTask<DialogueResult>> PlayDialogue = PlayWithService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void ResetState()
        {
            activeScene = null;
        }

        // onFinished는 시작된 경우에만, 잠금이 풀린 뒤 한 번 불린다.
        public static bool Play(GameObject source, string refusalNodeId, Action onFinished = null)
        {
            if (IsActive || DeathSequence.IsActive || source == null)
                return false;

            DeathSceneEntry scene = Object.FindAnyObjectByType<DeathSceneEntry>();
            if (scene == null)
            {
                Debug.LogError($"[DeathRefusal] 씬에 DeathSceneEntry가 없어 거부 '{refusalNodeId}'를 처리하지 못했다.", source);
                return false;
            }

            InputLock inputLock = scene.InputLock;
            DialogueService dialogue = scene.Dialogue;
            // 대화·텔레포트·사망 중에는 거부하지 않는다.
            if ((inputLock != null && inputLock.IsLocked) || (dialogue != null && !dialogue.IsIdle))
                return false;

            activeScene = scene;
            if (inputLock != null)
                inputLock.Lock(InputLockReason.Refusal);

            IRefusalReaction[] reactions = source.GetComponents<IRefusalReaction>();
            Debug.Log($"[DeathRefusal] 거부 시작: {refusalNodeId} ({source.name})", source);
            Run(scene, inputLock, dialogue, scene.Laris, reactions, refusalNodeId, onFinished).Forget();
            return true;
        }

        static async YarnTask Run(DeathSceneEntry scene, InputLock inputLock, DialogueService dialogue,
            PlayerMovement laris, IRefusalReaction[] reactions, string refusalNodeId, Action onFinished)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refusalNodeId))
                {
                    Debug.LogWarning("[DeathRefusal] 거부 노드 이름이 비어 대사 없이 반응만 실행한다.");
                }
                else
                {
                    DialogueResult result = await PlayDialogue(dialogue, refusalNodeId);
                    if (result == DialogueResult.Rejected)
                        Debug.LogWarning($"[DeathRefusal] 거부 노드 '{refusalNodeId}'를 재생하지 못했다. 반응만 실행한다.");
                }

                foreach (IRefusalReaction reaction in reactions)
                {
                    // 대사 중 사망 리로드가 일어나면 옛 씬의 반응은 실행하지 않는다.
                    if (scene == null)
                        break;
                    if (!(reaction is Behaviour behaviour) || behaviour == null || !behaviour.isActiveAndEnabled)
                        continue;
                    await reaction.Run(laris);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (inputLock != null)
                    inputLock.Unlock(InputLockReason.Refusal);
                if (ReferenceEquals(activeScene, scene))
                    activeScene = null;
                Finish(onFinished);
            }
        }

        static void Finish(Action onFinished)
        {
            try
            {
                onFinished?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static async YarnTask<DialogueResult> PlayWithService(DialogueService dialogue, string nodeId)
        {
            if (dialogue == null)
            {
                Debug.LogWarning($"[DeathRefusal] DialogueService가 없어 거부 노드 '{nodeId}' 없이 진행한다.");
                return DialogueResult.Rejected;
            }
            return await dialogue.Play(nodeId);
        }
    }
}
