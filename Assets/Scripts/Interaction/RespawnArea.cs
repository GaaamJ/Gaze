using UnityEngine;
using ProjectT.Data;

namespace ProjectT.Interaction
{
    [RequireComponent(typeof(DialogueInteractable))]
    public class RespawnArea : MonoBehaviour
    {
        [Tooltip("이 리스폰 오브젝트가 있는 지역. 비어 있어도 리스폰 위치는 저장된다.")]
        [SerializeField] AreaData area;
        [Tooltip("다시 시작할 위치. 비우면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] Transform spawnPoint;

        public AreaData Area => area;

        public Vector2 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    }
}
