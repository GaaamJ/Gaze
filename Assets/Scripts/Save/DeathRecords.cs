using UnityEngine;

namespace ProjectT.Save
{
    public static class DeathRecords
    {
        public static bool Has(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;
            return SaveManager.Data.diedNodeIds.Contains(nodeId);
        }

        // 저장에 실패해도 메모리 값은 남아 다음 저장 때 함께 기록된다.
        public static bool Add(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return true;

            var ids = SaveManager.Data.diedNodeIds;
            if (ids.Contains(nodeId))
                return true;

            ids.Add(nodeId);
            if (SaveManager.Save())
                return true;

            Debug.LogWarning($"[DeathRecords] 사망 기록 저장에 실패했다. 메모리에만 남긴다: {nodeId}");
            return false;
        }
    }
}
