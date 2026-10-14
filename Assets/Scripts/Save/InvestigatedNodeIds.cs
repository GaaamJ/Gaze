namespace ProjectT.Save
{
    public static class InvestigatedNodeIds
    {
        // 이미 기록돼 있으면 다시 저장하지 않는다. 저장에 실패해도 메모리 값은 남아 다음 저장 때 함께 기록된다.
        public static bool Add(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return true;

            var ids = SaveManager.Data.investigatedNodeIds;
            if (ids.Contains(nodeId))
                return true;

            ids.Add(nodeId);
            return SaveManager.Save();
        }
    }
}
