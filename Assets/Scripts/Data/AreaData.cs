using UnityEngine;
using ProjectT.Core.GameFolder;

namespace ProjectT.Data
{
    [CreateAssetMenu(fileName = "AreaData", menuName = "ProjectT/Area Data")]
    public class AreaData : ScriptableObject
    {
        [Tooltip("지역 고유 이름. 씬 마커가 이 지역을 가리킬 때 쓴다.")]
        [SerializeField] string areaId;

        [Tooltip("게임 폴더 Areas 아래의 층 폴더 이름. 예) Temp")]
        [SerializeField] string floorFolderName;

        public string AreaId => areaId;
        public string FloorFolderName => floorFolderName;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(areaId))
                Debug.LogWarning($"[AreaData] '{name}': 지역 id가 비어 있다.", this);

            if (!GameFolderPaths.TryNormalizeFolderName(floorFolderName, out _))
                Debug.LogWarning($"[AreaData] '{name}': 층 폴더 이름 '{floorFolderName}'을 폴더 이름으로 쓸 수 없다.", this);
        }
#endif
    }
}
