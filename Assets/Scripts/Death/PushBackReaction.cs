using UnityEngine;
using Yarn.Unity;
using ProjectT.Player;

namespace ProjectT.Death
{
    public class PushBackReaction : MonoBehaviour, IRefusalReaction
    {
        const float OverlapEpsilon = 0.001f;
        const float StopEpsilon = 0.0001f;

        [Tooltip("이 오브젝트의 중심에서 멀어지는 방향으로 물러날 거리(유닛). 거부 영역 안으로 파고든 깊이보다 커야 물러난 뒤 영역 밖이 된다.")]
        [SerializeField] float distance = 1f;
        [Tooltip("물러나는 데 걸리는 시간(초)")]
        [SerializeField] float duration = 0.3f;

        readonly RaycastHit2D[] hits = new RaycastHit2D[8];

        public float Distance => distance;

        public async YarnTask Run(PlayerMovement laris)
        {
            if (laris == null || !laris.TryGetComponent(out Rigidbody2D body))
            {
                Debug.LogWarning($"[PushBackReaction] '{name}': 라리스를 찾지 못해 물러나지 않는다.", this);
                return;
            }

            Collider2D bodyCollider = FindBodyCollider(body);
            Vector2 direction = AwayDirection(transform.position, body.position, laris.Facing);
            float moved = 0f;
            float elapsed = 0f;

            while (moved < distance)
            {
                await Awaitable.FixedUpdateAsync();
                if (this == null || body == null)
                    return;

                elapsed += Time.fixedDeltaTime;
                float target = duration > 0f ? distance * Mathf.Clamp01(elapsed / duration) : distance;
                float step = target - moved;
                float free = FreeDistance(bodyCollider, direction, step);
                body.MovePosition(body.position + direction * free);
                moved += free;
                if (free + StopEpsilon < step)
                    break;
            }

            // MovePosition은 다음 물리 단계에 반영되므로 잠금을 풀기 전에 한 단계 기다린다.
            await Awaitable.FixedUpdateAsync();
        }

        internal static Vector2 AwayDirection(Vector2 center, Vector2 laris, Vector2Int facing)
        {
            Vector2 offset = laris - center;
            if (offset.sqrMagnitude > OverlapEpsilon * OverlapEpsilon)
                return offset.normalized;
            return facing == Vector2Int.zero ? Vector2.up : -((Vector2)facing).normalized;
        }

        internal static Vector2 Destination(Vector2 center, Vector2 laris, Vector2Int facing, float distance)
        {
            return laris + AwayDirection(center, laris, facing) * distance;
        }

        // 조사 감지 원(트리거)까지 Cast하면 벽보다 먼저 걸리므로 몸 콜라이더만 쓴다.
        static Collider2D FindBodyCollider(Rigidbody2D body)
        {
            foreach (Collider2D collider in body.GetComponentsInChildren<Collider2D>())
            {
                if (!collider.isTrigger && collider.attachedRigidbody == body)
                    return collider;
            }
            return null;
        }

        float FreeDistance(Collider2D bodyCollider, Vector2 direction, float step)
        {
            if (bodyCollider == null || step <= 0f)
                return Mathf.Max(0f, step);

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(bodyCollider.gameObject.layer));
            float skin = Physics2D.defaultContactOffset;
            int count = bodyCollider.Cast(direction, filter, hits, step + skin, true);

            float free = step;
            for (int i = 0; i < count; i++)
            {
                // 이미 겹쳐 있다가 멀어지는 콜라이더는 막지 않는다.
                if (Vector2.Dot(hits[i].normal, direction) >= 0f)
                    continue;
                free = Mathf.Min(free, Mathf.Max(0f, hits[i].distance - skin));
            }
            return free;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (distance <= 0f)
                Debug.LogWarning($"[PushBackReaction] '{name}': distance가 0 이하라 물러나지 않는다.", this);
            if (duration < 0f)
                Debug.LogWarning($"[PushBackReaction] '{name}': duration이 음수라 한 번에 물러난다.", this);
        }
#endif
    }
}
