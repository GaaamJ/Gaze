using UnityEngine;
using ProjectT.Data;

namespace ProjectT.Interaction
{
    [RequireComponent(typeof(DialogueInteractable))]
    public class RespawnArea : MonoBehaviour
    {
        [Tooltip("이 리스폰 오브젝트가 있는 지역. 조사하면 이 지역 id가 리스폰 지점으로 저장된다.")]
        [SerializeField] AreaData area;

        public AreaData Area => area;
    }
}
