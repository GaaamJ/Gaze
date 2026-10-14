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
        [SerializeField] InteractableData data;

        public InteractableData Data => data;
        public bool CanInteract => data != null && isActiveAndEnabled;

        void Awake()
        {
            if (data == null)
                Debug.LogWarning($"[Interactable] '{name}': InteractableData가 비어 있어 조사 대상에서 제외한다.", this);
        }

        public void Interact(InteractContext context)
        {
            // 대사 중 '게임 끄기'로 종료해도 갱신된 지점이 남도록 대사보다 먼저 저장한다.
            if (TryGetComponent(out RespawnArea respawn))
                UpdateRespawn(respawn);

            if (string.IsNullOrWhiteSpace(data.DialogueNode))
                return;

            // await 없이 시작해야 Play가 같은 콜백 안에서 입력 잠금을 건다. 예외는 Forget()이 로그로 남긴다.
            PlayDialogue(context).Forget();
        }

        public string ResolveDialogueNode(ICollection<InteractableData> investigated)
        {
            if (investigated.Contains(data) && !string.IsNullOrWhiteSpace(data.RecheckDialogueNode))
                return data.RecheckDialogueNode;
            return data.DialogueNode;
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

            InteractableData record = data;
            string node = ResolveDialogueNode(context.Investigated);

            DialogueResult result = await context.Dialogue.Play(node);

            // 대사 중 이 오브젝트가 파괴될 수 있으므로 await 뒤에는 미리 받아 둔 값만 쓴다.
            if (result == DialogueResult.Completed)
                context.Investigated.Add(record);
        }
    }
}
