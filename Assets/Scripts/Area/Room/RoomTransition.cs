using UnityEngine;

namespace ProjectT.Area
{
    // Room 씬의 RoomDoor가 Core의 RoomLoader를 인스펙터로 참조할 수 없어 여기서 찾아 넘긴다.
    public static class RoomTransition
    {
        public static bool Begin(string targetRoom, string targetDoorId)
        {
            RoomLoader loader = Object.FindAnyObjectByType<RoomLoader>();
            if (loader == null)
            {
                Debug.LogError($"[RoomTransition] 씬에 RoomLoader가 없어 Room '{targetRoom}'로 넘어가지 못했다.");
                return false;
            }
            return loader.BeginMove(targetRoom, targetDoorId);
        }
    }
}
