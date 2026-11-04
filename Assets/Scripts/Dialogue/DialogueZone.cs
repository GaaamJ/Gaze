using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;
using Gaze.Data;
using Gaze.Death;
using Gaze.Interaction;
using Gaze.Player;
using Gaze.Session;

namespace Gaze.Dialogue
{
    // 라리스가 처음 들어오면 Yarn 대사를 한 번 재생하는 영역. Ex. 첫 만남, 시체 앞 외면 대사.
    // 끝까지 보면 조사 기록에 남아 다시 재생하지 않음. 라리스를 막는 건 이 영역이 아니라 같은 프리팹의 콜라이더가 함.
    // 이미 영역 안에서 시작해도 재생되도록 Enter가 아니라 Stay에서 확인함.
    [RequireComponent(typeof(Collider2D))]
    public class DialogueZone : MonoBehaviour
    {
        [Tooltip("처음 들어오면 한 번 재생할 Yarn 노드 이름 (예: Death_Falling001_Refuse). 끝까지 보면 조사 기록에 남아 다시 재생하지 않음.")]
        [FormerlySerializedAs("refusalNodeId")]
        [SerializeField] string dialogueNodeId;

        bool playing;
        // 재생하지 못했으면 영역을 나갔다 다시 들어올 때까지 다시 시도하지 않음. Stay에서 경고가 쌓이지 않게.
        bool rejected;

        // 에디트 모드 테스트에서는 DialogueRunner가 돌지 않아 바꿔 끼운다.
        internal static Func<string, YarnTask<DialogueResult>> PlayDialogue = PlayWithSceneDialogue;

        void OnTriggerStay2D(Collider2D other) => HandleInside(other);

        void OnTriggerExit2D(Collider2D other) => HandleExited(other);

        // 라리스가 영역을 나갔으면 재생 실패 기록을 지움. 에디트 모드 테스트가 직접 부름.
        internal void HandleExited(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                rejected = false;
        }

        // 라리스가 영역 안에 있고, 재생 중·사망 중이 아니고, 아직 조사 기록에 없으면 대사를 재생함.
        // 다른 대화 중이면 재생하지 않고 넘어가며, 영역 안에 있는 동안 대화가 끝나면 다음 확인에서 재생함.
        internal void HandleInside(Collider2D other)
        {
            if (!PlayerMovement.TryFrom(other, out _) || playing || rejected || DeathSequence.IsActive)
                return;

            HashSet<string> investigated = GameSession.Investigated;
            if (string.IsNullOrWhiteSpace(dialogueNodeId) || investigated == null || investigated.Contains(dialogueNodeId))
                return;

            // await 없이 시작해야 DialogueService.Play가 같은 콜백 안에서 입력 잠금을 건다.
            Play(dialogueNodeId, investigated).Forget();
        }

        // 대사 재생 → 끝까지 봤으면 조사 기록에 남김.
        // 재생 중에는 playing으로 다시 들어오는 것을 막음.
        async YarnTask Play(string nodeId, HashSet<string> investigated)
        {
            playing = true;
            try
            {
                DialogueResult result = await PlayDialogue(nodeId);
                if (result == DialogueResult.Completed)
                {
                    DialogueResponse.RecordInvestigated(investigated, nodeId);
                }
                else if (result == DialogueResult.Rejected)
                {
                    rejected = true;
                    Debug.LogWarning($"[DialogueZone] 노드 '{nodeId}'를 재생하지 못했다.");
                }
            }
            finally
            {
                playing = false;
            }
        }

        // 씬의 DialogueService로 노드를 재생. 서비스가 없으면 Rejected, 다른 대화 중이면 Interrupted를 돌려줌.
        static async YarnTask<DialogueResult> PlayWithSceneDialogue(string nodeId)
        {
            DialogueService dialogue = FindAnyObjectByType<DialogueService>();
            if (dialogue == null)
            {
                Debug.LogWarning($"[DialogueZone] 씬에 DialogueService가 없어 '{nodeId}'를 재생하지 못했다.");
                return DialogueResult.Rejected;
            }
            // 다른 대화 중이면 DialogueService가 경고와 함께 거부하므로 미리 조용히 넘긴다.
            if (!dialogue.IsIdle)
                return DialogueResult.Interrupted;
            return await dialogue.Play(nodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (TryGetComponent(out Collider2D zone) && !zone.isTrigger)
                Debug.LogWarning($"[DialogueZone] '{name}': 콜라이더가 Trigger가 아니라 라리스가 들어갈 수 없다. 막는 콜라이더는 자식에 둔다.", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DialogueZone), dialogueNodeId);
        }
#endif
    }
}
