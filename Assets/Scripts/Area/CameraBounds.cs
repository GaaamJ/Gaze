using Unity.Cinemachine;
using UnityEngine;
using ProjectT.Data;

namespace ProjectT.Area
{
    // 카메라 경계 마커는 같은 오브젝트의 BoxCollider2D·PolygonCollider2D로 구역 범위를 나타낸다.
    [RequireComponent(typeof(CinemachineConfiner2D))]
    public class CameraBounds : MonoBehaviour
    {
        [SerializeField] AreaMarkerRegistry registry;

        public Collider2D Current => GetComponent<CinemachineConfiner2D>().BoundingShape2D;

        void Awake()
        {
            if (registry == null)
                Debug.LogError("[CameraBounds] 'registry' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
        }

        public bool TrySwitch(AreaData area, string boundsMarkerId)
        {
            if (registry == null || !registry.TryGet(area, boundsMarkerId, out var marker))
            {
                Debug.LogWarning($"[CameraBounds] 카메라 경계 마커 {(area != null ? area.AreaId : "(지역 없음)")}/{boundsMarkerId}를 찾지 못해 경계를 바꾸지 않는다.", this);
                return false;
            }
            if (!marker.TryGetComponent(out Collider2D shape))
            {
                Debug.LogWarning($"[CameraBounds] '{marker.name}'에 Collider2D가 없어 경계를 바꾸지 않는다.", marker);
                return false;
            }

            Switch(shape);
            return true;
        }

        public void Switch(Collider2D shape)
        {
            var confiner = GetComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = shape;
            confiner.InvalidateBoundingShapeCache();
            // 이전 구역 위치에서 감쇠로 따라오면 옆 구역이 비치므로 다음 프레임에 바로 붙인다.
            if (TryGetComponent(out CinemachineCamera cinemachineCamera))
                cinemachineCamera.PreviousStateIsValid = false;
        }
    }
}
