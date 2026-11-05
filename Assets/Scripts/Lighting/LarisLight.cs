using UnityEngine;

namespace Gaze.Lighting
{
    // 라리스의 빛이 닿는 범위를 나타내는 Trigger 콜라이더에 붙는 표시.
    // 빛 오브젝트(LightBreath)와 형제로 둬서 숨쉬기에 따라 범위가 흔들리지 않음.
    // Room 씬의 부품이 OnTrigger 콜백에서 Is로 빛 범위인지 알아봄.
    [RequireComponent(typeof(Collider2D))]
    public class LarisLight : MonoBehaviour
    {
        // 라리스의 Rigidbody에 붙은 콜라이더라, Trigger가 아니면 이 범위 전체가 벽에 부딪힘.
        void Awake()
        {
            if (!GetComponent<Collider2D>().isTrigger)
                Debug.LogError("[LarisLight] 콜라이더가 Trigger가 아니다. 라리스가 빛 범위만큼 벽에 막힌다.", this);
        }

        // other가 라리스의 빛 범위 콜라이더면 참.
        // PlayerMovement.TryFrom은 Trigger를 거르므로 빛 범위는 이걸로만 알아봄.
        public static bool Is(Collider2D other) => other != null && other.TryGetComponent(out LarisLight _);
    }
}
