using UnityEngine;

namespace Gaze.Area
{
    // Room 씬의 Door가 Core의 RoomSceneLoader를 인스펙터로 참조할 수 없어 여기서 찾아 넘김.
    public static class RoomSceneTransition
    {
        // 씬에서 RoomSceneLoader를 찾아 Room 이동을 시작시킴. Door.TryEnter가 부름.
        // fromDoorId는 들어간 문의 id. 도착 문이 어디서 왔는지 알 수 있게 넘김.
        // 로더가 없거나 로더가 이동을 받지 않으면 false.
        public static bool Begin(string targetScene, string targetDoorId, string fromDoorId)
        {
            RoomSceneLoader loader = Object.FindAnyObjectByType<RoomSceneLoader>();
            if (loader == null)
            {
                Debug.LogError($"[RoomSceneTransition] 씬에 RoomSceneLoader가 없어 Room '{targetScene}'로 넘어가지 못했다.");
                return false;
            }
            return loader.BeginMove(targetScene, targetDoorId, fromDoorId);
        }
    }
}
