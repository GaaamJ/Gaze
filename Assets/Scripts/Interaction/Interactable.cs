using UnityEngine;

namespace ProjectT.Interaction
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(InteractContext context);
    }
}
