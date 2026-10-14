using UnityEngine;
using ProjectT.Interaction;

namespace ProjectT.Puzzle
{
    [RequireComponent(typeof(Interactable))]
    public class DangerousGap : MonoBehaviour, IInteractionOverride
    {
        public void Interact(InteractContext context)
        {
            Debug.Log("[DangerousGap] 사망 발동 지점 (#52 연결 전)", this);
        }
    }
}
