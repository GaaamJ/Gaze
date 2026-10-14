using UnityEngine;

namespace ProjectT.Data
{
    [CreateAssetMenu(fileName = "InteractableData", menuName = "ProjectT/Interactable Data")]
    public class InteractableData : ScriptableObject
    {
        [Tooltip("첫 조사 때 재생할 Yarn 노드 이름. 비우면 대사 없이 조사만 된다.")]
        [SerializeField] string dialogueNode;

        [Tooltip("다시 조사하면 재생할 대사 노드 이름. 비우면 첫 대사를 반복한다.")]
        [SerializeField] string recheckDialogueNode;

        public string DialogueNode => dialogueNode;
        public string RecheckDialogueNode => recheckDialogueNode;

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(InteractableData), dialogueNode, recheckDialogueNode);
        }
#endif
    }
}
