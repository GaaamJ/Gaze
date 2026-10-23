using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;
using ProjectT.Data;
using ProjectT.Dialogue;
using ProjectT.Save;

namespace ProjectT.Interaction
{
    public class DialogueInteractable : Interactable
    {
        [Tooltip("첫 조사 때 재생할 Yarn 노드 이름. 재조사는 '{dialogueNodeId}_Recheck'가 있으면 그것, 없으면 첫 노드를 반복한다. 비우면 대사 없이 조사만 된다.")]
        [FormerlySerializedAs("nodeId")]
        [SerializeField] string dialogueNodeId;

        public string DialogueNodeId => dialogueNodeId;

        public override void Interact(InteractContext context)
        {
            // 대사 중 '게임 끄기'로 종료해도 갱신된 지점이 남도록 대사보다 먼저 저장한다.
            if (TryGetComponent(out Respawn respawn))
                UpdateRespawn(respawn);

            if (string.IsNullOrWhiteSpace(dialogueNodeId))
                return;

            // await 없이 시작해야 Play가 같은 콜백 안에서 입력 잠금을 건다. 예외는 Forget()이 로그로 남긴다.
            PlayDialogue(context).Forget();
        }

        public string ResolveDialogueNode(ICollection<string> investigated, Func<string, bool> nodeExists)
        {
            if (!investigated.Contains(dialogueNodeId))
                return dialogueNodeId;

            string recheck = DialogueNodeNames.Recheck(dialogueNodeId);
            return nodeExists(recheck) ? recheck : dialogueNodeId;
        }

        void UpdateRespawn(Respawn respawn)
        {
            Vector2 position = respawn.SpawnPosition;
            string roomName = gameObject.scene.name;
            if (RespawnPoint.Set(position, roomName))
                Debug.Log($"[DialogueInteractable] 리스폰 지점 갱신·저장: {position} ({roomName})", this);
        }

        async YarnTask PlayDialogue(InteractContext context)
        {
            if (context.Dialogue == null)
            {
                Debug.LogWarning($"[DialogueInteractable] '{name}': DialogueService가 없어 대사를 재생하지 못했다.", this);
                return;
            }

            string record = dialogueNodeId;
            string node = ResolveDialogueNode(context.Investigated, context.Dialogue.HasNode);

            DialogueResult result = await context.Dialogue.Play(node);

            // 대사 중 이 오브젝트가 파괴될 수 있으므로 await 뒤에는 미리 받아 둔 값만 쓴다.
            if (result == DialogueResult.Completed)
                RecordInvestigated(context.Investigated, record);
        }

        internal static void RecordInvestigated(ICollection<string> investigated, string nodeId)
        {
            investigated.Add(nodeId);
            InvestigatedNodeIds.Add(nodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DialogueInteractable), dialogueNodeId);
        }
#endif
    }
}
