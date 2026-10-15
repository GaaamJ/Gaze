using System;
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
        [Tooltip("거부가 끝난 뒤(잠금이 풀린 시점부터) 다시 들어와도 재생하지 않는 시간(초). 0이면 나갔다 들어오는 즉시 다시 재생한다.")]
        [SerializeField] float retryDelay = 0.7f;

        bool larisInside;
        float retryAt = float.NegativeInfinity;

        // 에디트 모드 테스트에서 시간을 바꿔 끼운다.
        internal Func<float> Clock = () => Time.realtimeSinceStartup;

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
            // 대기 중에 들어온 것도 들어온 것으로 기록해, 대기가 끝나도 나갔다 들어와야 재생되게 한다.
            larisInside = true;
            if (Clock() < retryAt)
                return;
            DeathRefusal.Play(gameObject, refusalNodeId, OnRefusalFinished);
        }

        internal void HandleExited(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                larisInside = false;
        }

        void OnRefusalFinished()
        {
            if (this == null)
                return;
            retryAt = Clock() + Mathf.Max(0f, retryDelay);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (TryGetComponent(out Collider2D zone) && !zone.isTrigger)
                Debug.LogWarning($"[DeathRefusalZone] '{name}': 콜라이더가 Trigger가 아니라 라리스가 들어갈 수 없다.", this);
            if (retryDelay < 0f)
                Debug.LogWarning($"[DeathRefusalZone] '{name}': retryDelay가 음수라 0으로 본다.", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathRefusalZone), refusalNodeId);
        }
#endif
    }
}
