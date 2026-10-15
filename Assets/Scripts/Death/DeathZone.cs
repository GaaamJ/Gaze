using UnityEngine;
using ProjectT.Data;
using ProjectT.Player;

namespace ProjectT.Death
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour, IDeathSource
    {
        [Tooltip("라리스가 들어오면 재생할 사망 노드 이름 (예: Death_FallingDeath)")]
        [SerializeField] string deathNodeId;

        public string DeathNodeId => deathNodeId;

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleEntered(other);
        }

        internal void HandleEntered(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                DeathSequence.Begin(deathNodeId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (TryGetComponent(out Collider2D zone) && !zone.isTrigger)
                Debug.LogWarning($"[DeathZone] '{name}': 콜라이더가 Trigger가 아니라 라리스가 들어갈 수 없다.", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathZone), deathNodeId);
        }
#endif
    }
}
