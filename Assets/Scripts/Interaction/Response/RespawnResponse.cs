using UnityEngine;
using Gaze.Save;
using Gaze.Switches;

namespace Gaze.Interaction
{
    // 조사되면 이 오브젝트가 있는 Room과 위치를 리스폰 지점으로 저장하는 부품.
    // 그때까지 켠 스위치도 함께 확정함. 여기서 확정하지 않은 스위치는 죽으면 꺼짐.
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
            // 스위치를 먼저 세이브 내용에 확정해 두면 아래 저장 한 번에 위치와 스위치가 같이 기록됨.
            SwitchRecords.CopyToSave();
            if (RespawnPoint.Set(position, roomName))
                Debug.Log($"[RespawnResponse] 리스폰 지점 갱신·저장: {position} ({roomName})", this);
        }
    }
}
