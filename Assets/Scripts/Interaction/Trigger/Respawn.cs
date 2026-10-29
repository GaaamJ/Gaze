using UnityEngine;

namespace ProjectT.Interaction
{
    // 조사하면 리스폰 지점이 되는 오브젝트에 붙이는 표시 컴포넌트.
    // 위치만 들고 있고, 저장은 DialogueInteractable.Interact가 함.
    [RequireComponent(typeof(DialogueInteractable))]
    public class Respawn : MonoBehaviour
    {
        [Tooltip("다시 시작할 위치. 비우면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] Transform spawnPoint;

        public Vector2 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    }
}
