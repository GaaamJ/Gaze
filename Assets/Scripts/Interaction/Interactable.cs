using UnityEngine;

namespace ProjectT.Interaction
{
    // 조사 대상 컴포넌트의 공통 부모. 켜져 있는 동안만 조사할 수 있음.
    // DialogueInteractable, DeathInteractable이 상속함.
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(InteractContext context);
    }
}
