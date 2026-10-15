using UnityEngine;
using ProjectT.Data;
using ProjectT.Player;

namespace ProjectT.Death
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathRefusalZone : MonoBehaviour
    {
        [Tooltip("라리스가 들어오면 재생할 거부 노드 이름 (예: Death_Falling001_Refuse)")]
        [SerializeField] string refusalNodeId;

        bool larisInside;

        public string RefusalNodeId => refusalNodeId;

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleEntered(other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            HandleExited(other);
        }

        // 꺼진 동안에는 나가는 이벤트가 오지 않으므로 다시 켜질 때 새로 들어온 것으로 본다.
        void OnDisable()
        {
            larisInside = false;
        }

        internal void HandleEntered(Collider2D other)
        {
            if (!PlayerMovement.TryFrom(other, out _) || larisInside)
                return;
            larisInside = true;
            DeathRefusal.Play(gameObject, refusalNodeId);
        }

        internal void HandleExited(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                larisInside = false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (TryGetComponent(out Collider2D zone) && !zone.isTrigger)
                Debug.LogWarning($"[DeathRefusalZone] '{name}': 콜라이더가 Trigger가 아니라 라리스가 들어갈 수 없다.", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathRefusalZone), refusalNodeId);
        }
#endif
    }
}
