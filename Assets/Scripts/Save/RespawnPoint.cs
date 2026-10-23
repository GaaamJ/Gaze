using UnityEngine;

namespace ProjectT.Save
{
    public static class RespawnPoint
    {
        public static bool TryGet(SaveData data, out Vector2 position)
        {
            position = default;
            if (data == null || !data.hasRespawnPosition)
                return false;
            position = data.respawnPosition;
            return true;
        }

        public static bool Set(Vector2 position, string areaId, string roomName)
        {
            SaveData data = SaveManager.Data;
            data.respawnPosition = position;
            data.hasRespawnPosition = true;
            data.respawnAreaId = areaId ?? "";
            data.roomName = roomName ?? "";
            return SaveManager.Save();
        }
    }
}
