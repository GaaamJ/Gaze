using UnityEngine;

namespace ProjectT.Save
{
    // 세이브된 리스폰 위치와 그 위치가 있는 Room 이름. DialogueInteractable이 조사할 때 Set으로 갱신함.
    public static class RespawnPoint
    {
        // 저장된 리스폰 위치를 꺼냄. 세이브가 없거나 위치가 기록된 적 없으면 거짓.
        public static bool TryGet(SaveData data, out Vector2 position)
        {
            position = default;
            if (data == null || !data.hasRespawnPosition)
                return false;
            position = data.respawnPosition;
            return true;
        }

        // 리스폰 위치와 Room 이름을 기록하고 저장. 저장 성공 여부를 돌려줌.
        public static bool Set(Vector2 position, string roomName)
        {
            SaveData data = SaveManager.Data;
            data.respawnPosition = position;
            data.hasRespawnPosition = true;
            data.roomName = roomName ?? "";
            return SaveManager.Save();
        }
    }
}
