using UnityEngine;
using Gaze.Data;
using Gaze.Interaction;

namespace Gaze.Death
{
    // 조사되면 사망이 시작되는 부품.
    [RequireComponent(typeof(Interactable))]
    public class DeathResponse : MonoBehaviour, IInteractResponse, IDeathSource
    {
        [Tooltip("조사하면 재생할 사망 노드 이름 (예: Death_DeathObject)")]
        [SerializeField] string deathNodeId;

        public string DeathNodeId => deathNodeId;

        // 조사 입력이 오면 DeathSequence.Begin으로 사망을 시작함.
        public void OnInteract(InteractContext context)
        {
            DeathSequence.Begin(deathNodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathResponse), deathNodeId);
        }
#endif
    }
}
