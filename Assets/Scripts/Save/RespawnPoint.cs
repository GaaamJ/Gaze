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

        // 저장에 실패해도 메모리 값은 바뀐 상태로 남아 다음 저장 때 함께 기록된다.
        public static bool Set(Vector2 position, string areaId)
        {
            SaveData data = SaveManager.Data;
            data.respawnPosition = position;
            data.hasRespawnPosition = true;
            if (!string.IsNullOrWhiteSpace(areaId))
                data.respawnAreaId = areaId;
            return SaveManager.Save();
        }
    }
}
