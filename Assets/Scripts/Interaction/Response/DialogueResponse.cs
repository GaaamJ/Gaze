using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;
using Gaze.Data;
using Gaze.Dialogue;
using Gaze.Save;

namespace Gaze.Interaction
{
    // 조사되면 Yarn 대사를 재생하는 부품.
    // 대사를 끝까지 보면 조사 기록에 남기고, 다음 조사부터는 재조사 노드를 씀.
    [RequireComponent(typeof(Interactable))]
    public class DialogueResponse : MonoBehaviour, IInteractResponse
    {
        [Tooltip("첫 조사 때 재생할 Yarn 노드 이름. 재조사는 '{dialogueNodeId}_Recheck'가 있으면 그것, 없으면 첫 노드를 반복한다. 비우면 대사 없이 조사만 된다.")]
        [FormerlySerializedAs("nodeId")]
        [SerializeField] string dialogueNodeId;

        public string DialogueNodeId => dialogueNodeId;

        // 노드 이름이 비었으면 아무것도 안 함.
        public void OnInteract(InteractContext context)
        {
            if (string.IsNullOrWhiteSpace(dialogueNodeId))
                return;

            // await 없이 시작해야 Play가 같은 콜백 안에서 입력 잠금을 검. 예외는 Forget()이 로그로 남김.
            PlayDialogue(context).Forget();
        }

        // 이번에 재생할 노드 이름을 고름. 처음이면 dialogueNodeId, 조사한 적 있으면 재조사 노드(없으면 dialogueNodeId).
        public string ResolveDialogueNode(ICollection<string> investigated, Func<string, bool> nodeExists)
        {
            if (!investigated.Contains(dialogueNodeId))
                return dialogueNodeId;

            string recheck = DialogueNodeNames.Recheck(dialogueNodeId);
            return nodeExists(recheck) ? recheck : dialogueNodeId;
        }

        // 재생할 노드 고르기 → 대사 재생 → 끝까지 봤으면 조사 기록.
        async YarnTask PlayDialogue(InteractContext context)
        {
            if (context.Dialogue == null)
            {
                Debug.LogWarning($"[DialogueResponse] '{name}': DialogueService가 없어 대사를 재생하지 못했다.", this);
                return;
            }

            string record = dialogueNodeId;
            string node = ResolveDialogueNode(context.Investigated, context.Dialogue.HasNode);

            DialogueResult result = await context.Dialogue.Play(node);

            // 대사 중 이 오브젝트가 파괴될 수 있으므로 await 뒤에는 미리 받아 둔 값만 씀.
            if (result == DialogueResult.Completed)
                RecordInvestigated(context.Investigated, record);
        }

        // 노드를 메모리 집합과 세이브 데이터(InvestigatedNodeIds) 양쪽에 조사 기록으로 남김.
        // DialogueZone도 부름.
        internal static void RecordInvestigated(ICollection<string> investigated, string nodeId)
        {
            investigated.Add(nodeId);
            InvestigatedNodeIds.Add(nodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DialogueResponse), dialogueNodeId);
        }
#endif
    }
}
