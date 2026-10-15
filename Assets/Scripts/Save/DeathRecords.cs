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

        public static bool Add(string nodeId) =>
            SaveManager.AddUnique(SaveManager.Data.diedNodeIds, nodeId);
    }
}
