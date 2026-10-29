namespace ProjectT.Data
{
    // Yarn 노드 이름 규칙을 한곳에 모음.
    public static class DialogueNodeNames
    {
        // 같은 대상을 다시 조사할 때 재생할 노드 이름. DialogueInteractable이 씀.
        public static string Recheck(string nodeId) => $"{nodeId}_Recheck";
    }
}
