using UnityEngine;

namespace ProjectT.Death
{
    // Room 씬의 RoomDoor가 Core의 CoreEntry를 인스펙터로 참조할 수 없어 여기서 찾아 넘긴다.
    public static class RoomTransition
    {
        public static bool Begin(string targetRoom, string targetDoorId)
        {
            CoreEntry core = Object.FindAnyObjectByType<CoreEntry>();
            if (core == null)
            {
                Debug.LogError($"[RoomTransition] 씬에 CoreEntry가 없어 Room '{targetRoom}'로 넘어가지 못했다.");
                return false;
            }
            return core.BeginRoomMove(targetRoom, targetDoorId);
        }
    }
}
