using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Data;
using ProjectT.Dialogue;
using ProjectT.Save;

namespace ProjectT.Interaction
{
    public class Interactable : MonoBehaviour, IInteractable
    {
        [Tooltip("첫 조사 때 재생할 Yarn 노드 이름. 재조사는 '{nodeId}_Recheck'가 있으면 그것, 없으면 첫 노드를 반복한다. 비우면 대사 없이 조사만 된다.")]
        [SerializeField] string nodeId;

        public string NodeId => nodeId;
        public bool CanInteract => isActiveAndEnabled;

        public void Interact(InteractContext context)
        {
            // 대사 중 '게임 끄기'로 종료해도 갱신된 지점이 남도록 대사보다 먼저 저장한다.
            if (TryGetComponent(out RespawnArea respawn))
                UpdateRespawn(respawn);

            if (TryGetComponent(out IInteractionOverride handler))
            {
                handler.Interact(context);
                return;
            }

            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            // await 없이 시작해야 Play가 같은 콜백 안에서 입력 잠금을 건다. 예외는 Forget()이 로그로 남긴다.
            PlayDialogue(context).Forget();
        }

        public string ResolveDialogueNode(ICollection<string> investigated, Func<string, bool> nodeExists)
        {
            string first = DialogueNodeNames.First(nodeId);
            if (!investigated.Contains(nodeId))
                return first;

            string recheck = DialogueNodeNames.Recheck(nodeId);
            return nodeExists(recheck) ? recheck : first;
        }

        void UpdateRespawn(RespawnArea respawn)
        {
            if (respawn.Area == null || string.IsNullOrWhiteSpace(respawn.Area.AreaId))
            {
                Debug.LogWarning($"[Interactable] '{name}': RespawnArea의 지역이 비어 있어 리스폰 지점을 갱신하지 않았다.", this);
                return;
            }

            string areaId = respawn.Area.AreaId;
            if (RespawnAreaIds.Set(areaId))
                Debug.Log($"[Interactable] 리스폰 지점 갱신·저장: {areaId}", this);
            else
                Debug.LogWarning($"[Interactable] 리스폰 지점 '{areaId}' 저장에 실패했다. 메모리 값만 갱신됐다.", this);
        }

        async YarnTask PlayDialogue(InteractContext context)
        {
            if (context.Dialogue == null)
            {
                Debug.LogWarning($"[Interactable] '{name}': DialogueService가 없어 대사를 재생하지 못했다.", this);
                return;
            }

            string record = nodeId;
            string node = ResolveDialogueNode(context.Investigated, context.Dialogue.HasNode);

            DialogueResult result = await context.Dialogue.Play(node);

            // 대사 중 이 오브젝트가 파괴될 수 있으므로 await 뒤에는 미리 받아 둔 값만 쓴다.
            if (result == DialogueResult.Completed)
                context.Investigated.Add(record);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(Interactable), nodeId);
        }
#endif
    }
}
