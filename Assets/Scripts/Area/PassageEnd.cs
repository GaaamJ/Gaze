using UnityEngine;

namespace ProjectT.Area
{
    public enum ArrivalFacing
    {
        Keep,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
    }

    // 도착 위치는 이 오브젝트의 위치다.
    [RequireComponent(typeof(Collider2D))]
    public class PassageEnd : MonoBehaviour
    {
        [Tooltip("이 끝에 도착한 뒤 라리스가 바라볼 방향. Keep이면 그대로 둔다.")]
        [SerializeField] ArrivalFacing arrivalFacing;

        [Tooltip("이 끝이 있는 방의 카메라 경계(BoxCollider2D·PolygonCollider2D·CompositeCollider2D). 비우면 경계를 바꾸지 않는다.")]
        [SerializeField] Collider2D cameraBounds;

        Passage passage;

        public ArrivalFacing ArrivalFacing => arrivalFacing;
        public Collider2D CameraBounds => cameraBounds;

        // 라리스가 이 끝에 도착한 뒤 트리거 밖으로 나가기 전까지 true다.
        public bool WaitingForExit { get; internal set; }

        void Awake()
        {
            passage = GetComponentInParent<Passage>();
            if (passage == null)
                Debug.LogError("[PassageEnd] 부모에서 Passage를 찾지 못했다.", this);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (passage != null)
                passage.OnEndEntered(this, other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (passage != null)
                passage.OnEndExited(this, other);
        }

        public bool TryGetFacing(out Vector2Int direction)
        {
            direction = arrivalFacing switch
            {
                ArrivalFacing.Up => Vector2Int.up,
                ArrivalFacing.Down => Vector2Int.down,
                ArrivalFacing.Left => Vector2Int.left,
                ArrivalFacing.Right => Vector2Int.right,
                ArrivalFacing.UpLeft => new Vector2Int(-1, 1),
                ArrivalFacing.UpRight => new Vector2Int(1, 1),
                ArrivalFacing.DownLeft => new Vector2Int(-1, -1),
                ArrivalFacing.DownRight => new Vector2Int(1, -1),
                _ => Vector2Int.zero,
            };
            return direction != Vector2Int.zero;
        }
    }
}
