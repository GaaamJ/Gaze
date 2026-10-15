using UnityEngine;
using Yarn.Unity;
using ProjectT.Data;
using ProjectT.Interaction;
using ProjectT.Save;

namespace ProjectT.Death
{
    public class DeathInteractable : Interactable, IDeathSource
    {
        [Tooltip("조사하면 재생할 사망 노드 이름 (예: Death_DangerousGap)")]
        [SerializeField] string deathNodeId;
        [Tooltip("이 사망 기록이 있으면 사망 대신 재생할 노드 이름 (예: DeathObject_Corpse). 비우면 다시 사망한다.")]
        [SerializeField] string afterDeathNodeId;

        public string DeathNodeId => deathNodeId;

        public override void Interact(InteractContext context)
        {
            if (!string.IsNullOrWhiteSpace(afterDeathNodeId) && DeathRecords.Has(deathNodeId))
            {
                PlayAfterDeath(context, afterDeathNodeId).Forget();
                return;
            }

            DeathSequence.Begin(deathNodeId);
        }

        async YarnTask PlayAfterDeath(InteractContext context, string node)
        {
            if (context.Dialogue == null)
            {
                Debug.LogWarning($"[DeathInteractable] '{name}': DialogueService가 없어 '{node}'를 재생하지 못했다.", this);
                return;
            }

            await context.Dialogue.Play(node);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathInteractable), deathNodeId, afterDeathNodeId);
        }
#endif
    }
}
