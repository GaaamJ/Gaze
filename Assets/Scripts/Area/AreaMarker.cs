using UnityEngine;
using ProjectT.Data;

namespace ProjectT.Area
{
    public class AreaMarker : MonoBehaviour
    {
        [Tooltip("이 마커가 속한 지역")]
        [SerializeField] AreaData area;

        [Tooltip("지역 안에서 고유한 마커 이름. 예) Respawn, Start, Elevator")]
        [SerializeField] string markerId;

        [Tooltip("출구 ↔ 도착 지점처럼 짝이 있는 마커만 연결한다.")]
        [SerializeField] AreaMarker pair;

        public AreaData Area => area;
        public string MarkerId => markerId;
        public AreaMarker Pair => pair;
        public Vector3 Position => transform.position;

        // 범위 마커는 같은 오브젝트의 Collider2D로 범위를 나타낸다.
        public bool TryGetBounds(out Bounds bounds)
        {
            if (TryGetComponent(out Collider2D range))
            {
                bounds = range.bounds;
                return true;
            }
            bounds = default;
            return false;
        }
    }
}
