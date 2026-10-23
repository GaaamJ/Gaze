using UnityEngine;
using ProjectT.Data;
using ProjectT.Interaction;

namespace ProjectT.Death
{
    public class DeathInteractable : Interactable, IDeathSource
    {
        [Tooltip("조사하면 재생할 사망 노드 이름 (예: Death_DeathObject)")]
        [SerializeField] string deathNodeId;

        public string DeathNodeId => deathNodeId;

        public override void Interact(InteractContext context)
        {
            DeathSequence.Begin(deathNodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathInteractable), deathNodeId);
        }
#endif
    }
}
