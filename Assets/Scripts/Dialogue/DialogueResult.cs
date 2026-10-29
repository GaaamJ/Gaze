namespace Gaze.Dialogue
{
    // DialogueService.Play의 결과.
    public enum DialogueResult
    {
        // 노드가 끝까지 재생됨(NodeComplete를 받음).
        Completed,
        // 시작하지 못함(inputLock 없음, 이미 대화 중, 노드 없음).
        Rejected,
        // RequestCancel 없이 끊김(NodeComplete 없이 종료).
        Interrupted,
        // RequestCancel로 끊김.
        Cancelled,
    }
}
