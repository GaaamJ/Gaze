using System;
using System.Collections.Generic;
using Gaze.Save;

namespace Gaze.Switches
{
    // 켜진 스위치를 기억하는 곳. 열쇠를 주웠다처럼 한 번 일어나면 이후 장면이 달라지는 일을 id로 들고 있음.
    // 켜는 쪽(SwitchResponse)은 TurnOn만, 읽는 쪽(LockedDoor, SwitchToggle)은 IsOn과 Changed만 봄.
    // 지금 켜진 스위치는 메모리에만 있고, 리스폰 지점에서 확정해야 세이브(onSwitchIds)에 남음.
    // 죽거나 다시 켜면 Revert가 마지막으로 확정한 상태로 되돌림. 연필(TurnOn) → 펜(Commit) → 지우개(Revert).
    // 네임스페이스 Gaze.Switches와 이름이 겹치지 않도록 클래스 이름에 Records를 붙임.
    public static class SwitchRecords
    {
        static readonly HashSet<string> on = new HashSet<string>();

        // 스위치가 새로 켜진 순간 부름. 이미 켜진 스위치를 다시 TurnOn하면 부르지 않음. Revert도 부르지 않음.
        // 구독하는 쪽은 켜질 때 IsOn으로 지금 상태를 먼저 보고, 이후 변화는 이 알림으로 받음.
        public static event Action<Switch> Changed;

        public static bool IsOn(Switch target) =>
            target != null && !string.IsNullOrEmpty(target.Id) && on.Contains(target.Id);

        // 스위치를 켬(메모리). 새로 켜졌으면 true.
        // 비었거나 id가 없는 스위치, 이미 켜진 스위치는 아무것도 하지 않고 false.
        public static bool TurnOn(Switch target)
        {
            if (target == null || string.IsNullOrEmpty(target.Id) || !on.Add(target.Id))
                return false;

            Changed?.Invoke(target);
            return true;
        }

        // 지금 켜진 스위치를 세이브 내용(onSwitchIds)에 확정하고 저장. 저장 성공 여부를 돌려줌.
        // 저장에 실패해도 메모리의 세이브 내용은 바뀐 채 남아 다음 저장 때 함께 기록됨.
        public static bool Commit()
        {
            CopyToSave();
            return SaveManager.Save();
        }

        // 지금 켜진 스위치를 세이브 내용(onSwitchIds)에만 확정함. 저장은 하지 않음.
        // RespawnResponse가 부름. 바로 이어지는 RespawnPoint.Set의 저장 한 번에 리스폰 위치와 스위치가 같이 기록됨.
        public static void CopyToSave()
        {
            List<string> committed = SaveManager.Data.onSwitchIds;
            committed.Clear();
            committed.AddRange(on);
            // 같은 내용이면 같은 파일이 나오도록 순서를 고정함.
            committed.Sort(StringComparer.Ordinal);
        }

        // 마지막으로 확정한 상태로 되돌림. 확정하지 않고 켠 스위치는 꺼진 것이 됨.
        // CoreEntry가 시작과 사망 후 재시작 때 Room을 불러오기 전에 부름. 그래서 Changed로 알리지 않음.
        // 읽는 쪽은 Room이 다시 불러와질 때 IsOn으로 새 상태를 봄.
        public static void Revert()
        {
            on.Clear();
            on.UnionWith(SaveManager.Data.onSwitchIds);
        }

        // 메모리의 기록만 지움. 세이브는 건드리지 않음. 테스트가 부름.
        internal static void Clear() => on.Clear();
    }
}
