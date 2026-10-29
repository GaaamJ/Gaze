using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Data;
using ProjectT.Dialogue;
using ProjectT.Interaction;
using ProjectT.Player;
using ProjectT.Session;

namespace ProjectT.Death
{
    // 라리스를 막는 건 같은 프리팹의 콜라이더가 함.
    // 이 영역은 처음 다가올 때 외면 대사만 한 번 재생함.
    [RequireComponent(typeof(Collider2D))]
    public class DeathRefusalZone : MonoBehaviour
    {
        [Tooltip("처음 들어오면 한 번 재생할 외면 Yarn 노드 이름 (예: Death_Falling001_Refuse). 끝까지 보면 조사 기록에 남아 다시 재생하지 않음.")]
        [SerializeField] string refusalNodeId;

        bool playing;

        // 에디트 모드 테스트에서는 DialogueRunner가 돌지 않아 바꿔 끼운다.
        internal static Func<string, YarnTask<DialogueResult>> PlayDialogue = PlayWithSceneDialogue;

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleEntered(other);
        }

        internal void HandleEntered(Collider2D other)
        {
            if (!PlayerMovement.TryFrom(other, out _) || playing || DeathSequence.IsActive)
                return;

            HashSet<string> investigated = GameSession.Investigated;
            if (string.IsNullOrWhiteSpace(refusalNodeId) || investigated == null || investigated.Contains(refusalNodeId))
                return;

            // await 없이 시작해야 DialogueService.Play가 같은 콜백 안에서 입력 잠금을 건다.
            Refuse(refusalNodeId, investigated).Forget();
        }

        async YarnTask Refuse(string nodeId, HashSet<string> investigated)
        {
            playing = true;
            try
            {
                DialogueResult result = await PlayDialogue(nodeId);
                if (result == DialogueResult.Completed)
                    DialogueInteractable.RecordInvestigated(investigated, nodeId);
                else if (result == DialogueResult.Rejected)
                    Debug.LogWarning($"[DeathRefusalZone] 거부 노드 '{nodeId}'를 재생하지 못했다.");
            }
            finally
            {
                playing = false;
            }
        }

        static async YarnTask<DialogueResult> PlayWithSceneDialogue(string nodeId)
        {
            DialogueService dialogue = FindAnyObjectByType<DialogueService>();
            if (dialogue == null)
            {
                Debug.LogWarning($"[DeathRefusalZone] 씬에 DialogueService가 없어 '{nodeId}'를 재생하지 못했다.");
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
                Debug.LogWarning($"[DeathRefusalZone] '{name}': 콜라이더가 Trigger가 아니라 라리스가 들어갈 수 없다. 막는 콜라이더는 자식에 둔다.", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathRefusalZone), refusalNodeId);
        }
#endif
    }
}
