using UnityEngine;
using ProjectT.Data;
using ProjectT.Interaction;

namespace ProjectT.Death
{
    public class DeathRefusalInteractable : Interactable
    {
        [Tooltip("조사하면 재생할 거부 노드 이름 (예: Death_DeathObject_Refuse)")]
        [SerializeField] string refusalNodeId;

        public string RefusalNodeId => refusalNodeId;

        public override void Interact(InteractContext context)
        {
            DeathRefusal.Play(gameObject, refusalNodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathRefusalInteractable), refusalNodeId);
        }
#endif
    }
}
