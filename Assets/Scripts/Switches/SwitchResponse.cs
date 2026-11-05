using UnityEngine;
using UnityEngine.Serialization;
using Gaze.Interaction;

namespace Gaze.Switches
{
    // 조사되면 스위치를 켜는 부품. 열쇠처럼 줍는 물건에 붙임.
    // SwitchRecords.TurnOn만 부르고, 그 뒤 무엇이 달라지는지(열쇠가 사라짐, 문이 열림)는 읽는 쪽이 정함.
    [RequireComponent(typeof(Interactable))]
    public class SwitchResponse : MonoBehaviour, IInteractResponse
    {
        [Tooltip("조사하면 켤 스위치. 이 스위치를 읽는 SwitchToggle, LockedDoor가 같은 애셋을 가리킨다.")]
        [FormerlySerializedAs("flag")]
        [SerializeField] Switch targetSwitch;

        // 이미 켜진 스위치여도 다시 불러도 됨. 알림은 처음 한 번만 감.
        public void OnInteract(InteractContext context)
        {
            if (targetSwitch == null)
            {
                Debug.LogWarning($"[SwitchResponse] '{name}': 'targetSwitch'가 비어 있어 스위치를 켜지 못했다.", this);
                return;
            }

            SwitchRecords.TurnOn(targetSwitch);
        }
    }
}
