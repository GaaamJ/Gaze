using UnityEngine;

namespace ProjectT.Player
{
    // 라리스의 바라보는 방향(PlayerMovement.Facing)에 맞는 스프라이트를 보여 줌.
    // 왼쪽 계열 그림만 있으므로 오른쪽 계열은 flipX로 좌우 반전해서 보여 줌.
    public class PlayerSprite : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite down;
        [SerializeField] Sprite up;
        [SerializeField] Sprite left;
        [SerializeField] Sprite leftDown;
        [SerializeField] Sprite leftUp;

        Vector2Int shownFacing;

        void Awake()
        {
            if (movement == null)
                Debug.LogError("[PlayerSprite] 'movement' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (spriteRenderer == null)
                Debug.LogError("[PlayerSprite] 'spriteRenderer' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (down == null)
                Debug.LogError("[PlayerSprite] 'down' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (up == null)
                Debug.LogError("[PlayerSprite] 'up' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (left == null)
                Debug.LogError("[PlayerSprite] 'left' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (leftDown == null)
                Debug.LogError("[PlayerSprite] 'leftDown' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (leftUp == null)
                Debug.LogError("[PlayerSprite] 'leftUp' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (movement == null || spriteRenderer == null || down == null || up == null
                || left == null || leftDown == null || leftUp == null)
                enabled = false;
        }

        void OnEnable()
        {
            Show(movement.Facing);
        }

        // PlayerMovement.Update에서 바뀐 방향을 같은 프레임에 반영하려고 LateUpdate에서 읽음.
        void LateUpdate()
        {
            if (movement.Facing != shownFacing)
                Show(movement.Facing);
        }

        // 위/아래/왼쪽 + 대각선 스프라이트를 고르고, 오른쪽 성분이 있으면 좌우 반전함.
        void Show(Vector2Int facing)
        {
            shownFacing = facing;

            if (facing.y > 0)
                spriteRenderer.sprite = facing.x == 0 ? up : leftUp;
            else if (facing.y < 0)
                spriteRenderer.sprite = facing.x == 0 ? down : leftDown;
            else
                spriteRenderer.sprite = left;

            spriteRenderer.flipX = facing.x > 0;
        }
    }
}
