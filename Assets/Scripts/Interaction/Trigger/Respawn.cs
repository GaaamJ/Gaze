using UnityEngine;

namespace ProjectT.Interaction
{
    [RequireComponent(typeof(DialogueInteractable))]
    public class Respawn : MonoBehaviour
    {
        [Tooltip("다시 시작할 위치. 비우면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] Transform spawnPoint;

        public Vector2 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    }
}
