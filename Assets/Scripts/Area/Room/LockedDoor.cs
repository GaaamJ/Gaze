using UnityEngine;
using Gaze.Events;

namespace Gaze.Area
{
    // 이벤트가 일어나야 열리는 문. 열쇠를 주운 뒤에야 들어갈 수 있는 방 같은 곳에 씀.
    // 행선지는 PairDoor와 같음. 들어갈 수 있는지만 EventManager.Has로 정함.
    // 잠겨 있을 때의 대사는 이 문이 재생하지 않음. 문 앞에 조사 대상을 따로 둠.
    public class LockedDoor : PairDoor
    {
        [Tooltip("이 이벤트가 일어나야 문이 열린다. 열쇠의 EventResponse와 같은 애셋.")]
        [SerializeField] EventFlag requiredFlag;

        // 닫혀 있는 동안 라리스가 문 칸에 닿을 때마다 불림. 이벤트가 일어났으면 열림.
        // requiredFlag가 비어 있으면 열 방법이 없으므로 잠가 두고 경고를 남김.
        protected override bool CanEnter()
        {
            if (requiredFlag == null)
            {
                Debug.LogWarning($"[LockedDoor] '{name}': 'requiredFlag'가 비어 있어 문을 열 수 없다.", this);
                return false;
            }

            return EventManager.Has(requiredFlag);
        }
    }
}
