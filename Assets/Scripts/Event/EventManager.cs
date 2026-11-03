using System;
using System.Collections.Generic;

namespace Gaze.Events
{
    // 일어난 이벤트를 기억하는 곳. 열쇠 줍기처럼 한 번 일어나면 이후 장면이 달라지는 일을 id로 들고 있음.
    // 남기는 쪽(EventResponse)은 Set만, 읽는 쪽(LockedDoor, EventToggle)은 Has와 Changed만 봄.
    // 지금은 메모리에만 둠. 리스폰 지점에서 확정하고 죽으면 되돌리는 규칙은 아직 없음.
    public static class EventManager
    {
        static readonly HashSet<string> happened = new HashSet<string>();

        // 이벤트가 새로 일어난 순간 부름. 이미 일어난 이벤트를 다시 Set하면 부르지 않음.
        // 구독하는 쪽은 켜질 때 Has로 지금 상태를 먼저 보고, 이후 변화는 이 이벤트로 받음.
        public static event Action<EventFlag> Changed;

        public static bool Has(EventFlag flag) =>
            flag != null && !string.IsNullOrEmpty(flag.Id) && happened.Contains(flag.Id);

        // 이벤트를 일어난 것으로 기록함. 새로 일어났으면 true.
        // 비었거나 id가 없는 이벤트, 이미 일어난 이벤트는 아무것도 하지 않고 false.
        public static bool Set(EventFlag flag)
        {
            if (flag == null || string.IsNullOrEmpty(flag.Id) || !happened.Add(flag.Id))
                return false;

            Changed?.Invoke(flag);
            return true;
        }

        // 기록을 모두 지움. 테스트가 부름.
        internal static void Clear() => happened.Clear();
    }
}
