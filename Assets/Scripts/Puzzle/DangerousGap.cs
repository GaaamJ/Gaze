using UnityEngine;
using ProjectT.Interaction;

namespace ProjectT.Puzzle
{
    public class DangerousGap : Interactable
    {
        public override void Interact(InteractContext context)
        {
            Debug.Log("[DangerousGap] 사망 발동 지점 (#52 연결 전)", this);
        }
    }
}
