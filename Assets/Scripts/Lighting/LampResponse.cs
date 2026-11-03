using UnityEngine;
using Gaze.Interaction;
using Gaze.Save;

namespace Gaze.Lighting
{
    // 조사되면 등불을 켜는 부품. 리스폰 지점에 RespawnResponse와 같이 붙임.
    // 켠 기록을 세이브에 남김 → 지금 불러와 있는 방 중 이 등불을 따르는 방을 밝힘.
    [RequireComponent(typeof(Interactable))]
    public class LampResponse : MonoBehaviour, IInteractResponse
    {
        [Tooltip("이 등불의 Lamp 애셋. 같은 애셋을 RoomLight에 넣은 방들이 밝아진다.")]
        [SerializeField] Lamp lamp;

        // 이미 켠 등불이어도 다시 불러도 됨. 기록은 한 번만 남고, 밝은 방은 그대로 밝음.
        public void OnInteract(InteractContext context)
        {
            if (lamp == null)
            {
                Debug.LogWarning($"[LampResponse] '{name}': 'lamp'가 비어 있어 등불을 켜지 못했다.", this);
                return;
            }

            LitLamps.Add(lamp.Id);
            RoomLight.LightUp(lamp);
        }
    }
}
