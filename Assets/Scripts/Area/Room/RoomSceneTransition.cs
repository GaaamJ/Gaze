using UnityEngine;

namespace ProjectT.Area
{
    // Room 씬의 RoomSceneDoor가 Core의 RoomSceneLoader를 인스펙터로 참조할 수 없어 여기서 찾아 넘긴다.
    public static class RoomSceneTransition
    {
        public static bool Begin(string targetScene, string targetDoorId)
        {
            RoomSceneLoader loader = Object.FindAnyObjectByType<RoomSceneLoader>();
            if (loader == null)
            {
                Debug.LogError($"[RoomSceneTransition] 씬에 RoomSceneLoader가 없어 Room '{targetScene}'로 넘어가지 못했다.");
                return false;
            }
            return loader.BeginMove(targetScene, targetDoorId);
        }
    }
}
