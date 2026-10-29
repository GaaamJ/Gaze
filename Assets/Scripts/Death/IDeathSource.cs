namespace Gaze.Death
{
    // 사망 노드 이름을 내놓는 트리거(DeathZone, DeathInteractable)의 공통 약속.
    // DeathAftermath가 이 이름으로 사망 기록을 찾음.
    public interface IDeathSource
    {
        string DeathNodeId { get; }
    }
}
