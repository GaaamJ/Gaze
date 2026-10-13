using UnityEngine;

namespace ProjectT.Area
{
    // 도착 지점은 같은 오브젝트 AreaMarker의 짝(pair)이다.
    [RequireComponent(typeof(AreaMarker))]
    public class ZoneExit : MonoBehaviour
    {
        [Tooltip("도착 구역의 카메라 경계 마커 id. 도착 지점과 같은 지역에서 찾는다.")]
        [SerializeField] string arrivalCameraBoundsId;

        public AreaMarker Marker => GetComponent<AreaMarker>();
        public string ArrivalCameraBoundsId => arrivalCameraBoundsId;

        void OnTriggerEnter2D(Collider2D other)
        {
            // 라리스의 조사 감지 원(트리거)이 먼저 닿아도 넘어가지 않도록 몸 콜라이더만 본다.
            if (other.isTrigger || other.attachedRigidbody == null)
                return;
            if (other.attachedRigidbody.TryGetComponent(out ZoneTransition transition))
                transition.TryBegin(this);
        }
    }
}
