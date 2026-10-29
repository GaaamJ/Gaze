namespace Gaze.Save
{
    // 죽은 적 있는 사망 노드 이름의 기록(SaveData.diedNodeIds).
    // DeathSequence가 Add로 남기고 DeathAftermath가 Has로 확인함.
    public static class DeathRecords
    {
        // 노드로 죽은 적이 있는지. 공백 이름은 거짓.
        public static bool Has(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;
            return SaveManager.Data.diedNodeIds.Contains(nodeId);
        }

        // 사망 노드를 기록하고 저장. 저장 성공 여부를 돌려줌.
        public static bool Add(string nodeId) =>
            SaveManager.AddUnique(SaveManager.Data.diedNodeIds, nodeId);
    }
}
