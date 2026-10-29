using UnityEngine;
using ProjectT.Data;
using ProjectT.Interaction;

namespace ProjectT.Death
{
    // 조사하면 사망이 시작되는 오브젝트.
    public class DeathInteractable : Interactable, IDeathSource
    {
        [Tooltip("조사하면 재생할 사망 노드 이름 (예: Death_DeathObject)")]
        [SerializeField] string deathNodeId;

        public string DeathNodeId => deathNodeId;

        // 조사 입력이 오면 DeathSequence.Begin으로 사망을 시작함.
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
