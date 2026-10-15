namespace ProjectT.Save
{
    public static class InvestigatedNodeIds
    {
        public static bool Add(string nodeId) =>
            SaveManager.AddUnique(SaveManager.Data.investigatedNodeIds, nodeId);
    }
}
