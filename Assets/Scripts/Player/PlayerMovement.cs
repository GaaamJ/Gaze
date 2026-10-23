using UnityEngine;
using UnityEngine.InputSystem;
using ProjectT.Session;

namespace ProjectT.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputLock inputLock;
        [SerializeField] float moveSpeed = 3f;
        [Tooltip("대각선에서 키 하나를 뗐을 때 바라보는 방향을 상하좌우로 바꾸기 전 기다리는 시간(초)")]
        [SerializeField] float diagonalReleaseGrace = 0.1f;

        const float DeadZone = 0.5f;

        Rigidbody2D body;
        Vector2Int pendingFacing;
        float pendingSince;

        // 8방향. 각 성분은 -1, 0, 1. 멈춰 있으면 마지막 값을 유지한다.
        public Vector2Int Facing { get; private set; } = Vector2Int.down;

        // 라리스의 조사 감지 원(트리거)이 먼저 닿아도 반응하지 않도록 몸 콜라이더만 라리스로 본다.
        public static bool TryFrom(Collider2D other, out PlayerMovement laris)
        {
            laris = null;
            if (other == null || other.isTrigger || other.attachedRigidbody == null)
                return false;
            return other.attachedRigidbody.TryGetComponent(out laris);
        }

        public static void MoveTo(Rigidbody2D body, Vector3 position)
        {
            position.z = body.transform.position.z;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            // body.position은 다음 물리 단계에야 transform에 반영되므로, 카메라 스냅이 새 위치를 보도록 transform도 바로 옮긴다.
            body.transform.position = position;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            // 가만히 서 있는 동안에도 새로 나타난 조사 대상과 트리거 접촉이 생기도록 잠들지 않게 한다.
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (moveAction == null)
                Debug.LogError("[PlayerMovement] 'moveAction' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (inputLock == null)
                Debug.LogError("[PlayerMovement] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (moveAction == null || inputLock == null)
                enabled = false;
        }

        void OnEnable()
        {
            // 액션 애셋을 여러 곳이 공유하므로 OnDisable에서 Disable()하면 다른 사용처의 입력도 끊긴다.
            moveAction.action.Enable();
        }

        void OnDisable()
        {
            body.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            UpdateFacing(ReadDirection());
        }

        void FixedUpdate()
        {
            body.linearVelocity = ((Vector2)ReadDirection()).normalized * moveSpeed;
        }

        Vector2Int ReadDirection()
        {
            if (inputLock.IsLocked)
                return Vector2Int.zero;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            int x = input.x > DeadZone ? 1 : input.x < -DeadZone ? -1 : 0;
            int y = input.y > DeadZone ? 1 : input.y < -DeadZone ? -1 : 0;
            return new Vector2Int(x, y);
        }

        void UpdateFacing(Vector2Int direction)
        {
            if (direction == Vector2Int.zero || direction == Facing)
            {
                pendingFacing = Vector2Int.zero;
                return;
            }

            if (!IsReleasedFromDiagonal(direction))
            {
                Facing = direction;
                pendingFacing = Vector2Int.zero;
                return;
            }

            if (direction != pendingFacing)
            {
                pendingFacing = direction;
                pendingSince = Time.time;
            }

            if (Time.time - pendingSince >= diagonalReleaseGrace)
            {
                Facing = direction;
                pendingFacing = Vector2Int.zero;
            }
        }

        bool IsReleasedFromDiagonal(Vector2Int direction)
        {
            bool facingDiagonal = Facing.x != 0 && Facing.y != 0;
            bool directionCardinal = direction.x == 0 || direction.y == 0;
            return facingDiagonal && directionCardinal
                && (direction.x == Facing.x || direction.y == Facing.y);
        }
    }
}
