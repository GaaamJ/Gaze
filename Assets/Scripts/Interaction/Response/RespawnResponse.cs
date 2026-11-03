using UnityEngine;
using Gaze.Save;

namespace Gaze.Interaction
{
    // 조사되면 이 오브젝트가 있는 Room과 위치를 리스폰 지점으로 저장하는 부품.
    [RequireComponent(typeof(Interactable))]
    public class RespawnResponse : MonoBehaviour, IInteractResponse
    {
        [Tooltip("다시 시작할 위치. 비우면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] Transform spawnPoint;

        public Vector2 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        public void OnInteract(InteractContext context)
        {
            Vector2 position = SpawnPosition;
            string roomName = gameObject.scene.name;
            if (RespawnPoint.Set(position, roomName))
                Debug.Log($"[RespawnResponse] 리스폰 지점 갱신·저장: {position} ({roomName})", this);
        }
    }
}
