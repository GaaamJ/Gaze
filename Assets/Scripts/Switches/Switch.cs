using UnityEngine;

namespace Gaze.Switches
{
    // 스위치 하나를 가리키는 이름표 애셋. 켜졌는지는 들고 있지 않고 SwitchRecords가 Id로 기억함.
    // 스위치를 켜는 쪽(SwitchResponse)과 읽는 쪽(LockedDoor, SwitchToggle)이 같은 애셋을 가리켜 하나로 묶임.
    [CreateAssetMenu(fileName = "Switch", menuName = "Gaze/Switch")]
    public class Switch : ScriptableObject
    {
        [Tooltip("스위치를 구분하는 id. 이 애셋의 guid로 자동으로 채워진다.")]
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
