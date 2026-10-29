using UnityEngine;
using UnityEngine.InputSystem;
using ProjectT.Session;

namespace ProjectT.Player
{
    // 라리스의 8방향 이동과 바라보는 방향(Facing)을 맡음.
    // Update에서 Facing 갱신, FixedUpdate에서 속도 적용. 입력이 잠겨 있으면 멈춤.
    // DetectZone, PlayerSprite가 Facing을 읽음.
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

        // 8방향. 각 성분은 -1, 0, 1. 멈춰 있으면 마지막 값을 유지함.
        public Vector2Int Facing { get; private set; } = Vector2Int.down;

        // 트리거 콜라이더가 라리스의 몸인지 확인하고 PlayerMovement를 돌려줌.
        // 라리스의 조사 감지 원(트리거)이 먼저 닿아도 반응하지 않도록 몸 콜라이더만 라리스로 봄.
        // RoomSceneDoor, DeathZone, DeathRefusalZone이 부름.
        public static bool TryFrom(Collider2D other, out PlayerMovement laris)
        {
            laris = null;
            if (other == null || other.isTrigger || other.attachedRigidbody == null)
                return false;
            return other.attachedRigidbody.TryGetComponent(out laris);
        }

        // 라리스를 position으로 순간이동시키고 속도를 0으로 만듦. z는 유지함.
        // RoomSceneLoader, CoreEntry가 부름.
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
            // 가만히 서 있는 동안에도 새로 나타난 조사 대상과 트리거 접촉이 생기도록 잠들지 않게 함.
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
            // 액션 애셋을 여러 곳이 공유하므로 OnDisable에서 Disable()하면 다른 사용처의 입력도 끊김.
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

        // 이동 입력을 DeadZone으로 끊어 8방향(각 성분 -1, 0, 1)으로 바꿈. 입력이 잠겨 있으면 0.
        Vector2Int ReadDirection()
        {
            if (inputLock.IsLocked)
                return Vector2Int.zero;

            Vector2 input = moveAction.action.ReadValue<Vector2>();
            int x = input.x > DeadZone ? 1 : input.x < -DeadZone ? -1 : 0;
            int y = input.y > DeadZone ? 1 : input.y < -DeadZone ? -1 : 0;
            return new Vector2Int(x, y);
        }

        // 입력 방향으로 Facing을 바꿈. 입력이 없거나 같은 방향이면 대기 중이던 변경을 취소함.
        // 대각선을 보다가 키 하나를 뗀 경우는 diagonalReleaseGrace 동안 같은 입력이 이어져야 바꿈.
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

        // 대각선을 보던 중 키 하나를 떼서, 남은 키가 지금 Facing의 한 성분과 같은 상하좌우가 되었으면 참.
        bool IsReleasedFromDiagonal(Vector2Int direction)
        {
            bool facingDiagonal = Facing.x != 0 && Facing.y != 0;
            bool directionCardinal = direction.x == 0 || direction.y == 0;
            return facingDiagonal && directionCardinal
                && (direction.x == Facing.x || direction.y == Facing.y);
        }
    }
}
