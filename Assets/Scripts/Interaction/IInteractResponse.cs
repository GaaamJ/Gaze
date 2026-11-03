namespace Gaze.Interaction
{
    // 조사되면 실행되는 부품. 같은 오브젝트의 Interactable이 OnInteract를 부름.
    // DialogueResponse, RespawnResponse, DeathResponse가 구현함.
    public interface IInteractResponse
    {
        // 같은 프레임 안에 돌아와야 함. 오래 걸리는 일은 시작만 하고 기다리지 않음.
        void OnInteract(InteractContext context);
    }
}
