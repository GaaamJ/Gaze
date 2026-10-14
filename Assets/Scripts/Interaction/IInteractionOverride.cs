namespace ProjectT.Interaction
{
    // Interactable과 같은 오브젝트에 붙이면 대사 재생 대신 이쪽이 조사를 처리한다.
    public interface IInteractionOverride
    {
        void Interact(InteractContext context);
    }
}
