using UnityEngine;

namespace Gaze.Interaction
{
    // 조사 대상의 진입점. 오브젝트마다 하나 붙이고, 조사되면 같은 오브젝트의 IInteractResponse를 모두 실행함.
    // 무엇을 하는지(대사, 리스폰, 사망 등)는 모르고 부품에 맡김.
    // 켜져 있는 동안만 조사할 수 있음.
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour, IInteractable
    {
        public bool CanInteract => isActiveAndEnabled;

        // 꺼진 부품은 건너뜀.
        // 대사처럼 기다리는 부품도 시작만 하고 돌아오므로, 부품 순서와 상관없이 같은 프레임에 모두 실행됨.
        public void Interact(InteractContext context)
        {
            foreach (IInteractResponse response in GetComponents<IInteractResponse>())
            {
                if (response is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    continue;
                response.OnInteract(context);
            }
        }
    }
}
