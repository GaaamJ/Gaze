using UnityEngine;

namespace Gaze.Lighting
{
    // 등불 하나를 가리키는 이름표 애셋. 상태는 들고 있지 않음.
    // 등불의 LampResponse와 그 등불로 밝아질 방들의 RoomLight가 같은 애셋을 가리켜 하나로 묶임.
    // 켜졌는지는 세이브(LitLamps)에 Id로 남음.
    [CreateAssetMenu(fileName = "Lamp", menuName = "Gaze/Lamp")]
    public class Lamp : ScriptableObject
    {
        [Tooltip("세이브에 남는 id. 이 애셋의 guid로 자동으로 채워진다.")]
        [SerializeField] string id;

        public string Id => id;

#if UNITY_EDITOR
        // id를 이 애셋의 guid와 맞춤.
        // guid는 이름을 바꾸거나 옮겨도 그대로라 세이브가 유지되고, 복제한 애셋은 새 guid를 받아 id가 겹치지 않음.
        void OnValidate()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path))
                return;

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (id == guid)
                return;

            id = guid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
