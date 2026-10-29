namespace Gaze.Save
{
    // 조사를 마친 노드 이름의 기록(SaveData.investigatedNodeIds). DialogueInteractable이 남김.
    public static class InvestigatedNodeIds
    {
        // 노드를 기록하고 저장. 저장 성공 여부를 돌려줌.
        public static bool Add(string nodeId) =>
            SaveManager.AddUnique(SaveManager.Data.investigatedNodeIds, nodeId);
    }
}
