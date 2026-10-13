using System.Collections.Generic;
using UnityEngine;
using ProjectT.Data;

namespace ProjectT.Area
{
    public class AreaMarkerRegistry : MonoBehaviour
    {
        readonly MarkerIndex<AreaMarker> index = new();
        bool built;

        void Awake()
        {
            EnsureBuilt();
        }

        public bool TryGet(AreaData area, string markerId, out AreaMarker marker)
        {
            EnsureBuilt();
            if (area == null)
            {
                marker = null;
                return false;
            }
            return index.TryGet(area.AreaId, markerId, out marker) && marker != null;
        }

        public bool TryGetPosition(AreaData area, string markerId, out Vector3 position)
        {
            if (TryGet(area, markerId, out var marker))
            {
                position = marker.Position;
                return true;
            }
            position = default;
            return false;
        }

        public void Rebuild(IEnumerable<AreaMarker> markers)
        {
            index.Clear();
            built = true;
            foreach (var marker in markers)
            {
                if (marker == null)
                    continue;

                if (marker.Area == null || string.IsNullOrWhiteSpace(marker.MarkerId))
                {
                    Debug.LogWarning($"[AreaMarkerRegistry] '{marker.name}': 지역 또는 마커 id가 비어 있어 등록하지 않는다.", marker);
                    continue;
                }

                if (!index.TryAdd(marker.Area.AreaId, marker.MarkerId, marker))
                    Debug.LogWarning($"[AreaMarkerRegistry] '{marker.name}': {marker.Area.AreaId}/{marker.MarkerId} 마커가 이미 있어 무시한다.", marker);
            }
        }

        // 다른 오브젝트의 Awake에서 먼저 조회될 수 있어 첫 조회 때도 만든다.
        void EnsureBuilt()
        {
            if (built)
                return;
            Rebuild(FindObjectsByType<AreaMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }
    }
}
