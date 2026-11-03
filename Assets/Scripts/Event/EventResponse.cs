using UnityEngine;
using Gaze.Interaction;

namespace Gaze.Events
{
    // 조사되면 이벤트를 일어난 것으로 남기는 부품. 열쇠처럼 줍는 물건에 붙임.
    // EventManager.Set만 부르고, 그 뒤 무엇이 달라지는지(열쇠가 사라짐, 문이 열림)는 읽는 쪽이 정함.
    [RequireComponent(typeof(Interactable))]
    public class EventResponse : MonoBehaviour, IInteractResponse
    {
        [Tooltip("조사하면 일어날 이벤트. 이 이벤트를 읽는 EventToggle, LockedDoor가 같은 애셋을 가리킨다.")]
        [SerializeField] EventFlag flag;

        // 이미 일어난 이벤트여도 다시 불러도 됨. 알림은 처음 한 번만 감.
        public void OnInteract(InteractContext context)
        {
            if (flag == null)
            {
                Debug.LogWarning($"[EventResponse] '{name}': 'flag'가 비어 있어 이벤트를 남기지 못했다.", this);
                return;
            }

            EventManager.Set(flag);
        }
    }
}
