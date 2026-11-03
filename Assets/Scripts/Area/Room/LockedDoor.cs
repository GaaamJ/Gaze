using UnityEngine;
using UnityEngine.Serialization;
using Gaze.Switches;

namespace Gaze.Area
{
    // 스위치가 켜져야 열리는 문. 열쇠를 주운 뒤에야 들어갈 수 있는 방 같은 곳에 씀.
    // 행선지는 NormalDoor와 같음. 들어갈 수 있는지만 SwitchRecords.IsOn으로 정함.
    // 잠겨 있을 때의 대사는 이 문이 재생하지 않음. 문 앞에 조사 대상을 따로 둠.
    public class LockedDoor : NormalDoor
    {
        [Tooltip("이 스위치가 켜져야 문이 열린다. 열쇠의 SwitchResponse와 같은 애셋.")]
        [FormerlySerializedAs("requiredFlag")]
        [SerializeField] Switch requiredSwitch;

        // 닫혀 있는 동안 라리스가 문 칸에 닿을 때마다 불림. 스위치가 켜졌으면 열림.
        // requiredSwitch가 비어 있으면 열 방법이 없으므로 잠가 두고 경고를 남김.
        protected override bool CanEnter()
        {
            if (requiredSwitch == null)
            {
                Debug.LogWarning($"[LockedDoor] '{name}': 'requiredSwitch'가 비어 있어 문을 열 수 없다.", this);
                return false;
            }

            return SwitchRecords.IsOn(requiredSwitch);
        }
    }
}
