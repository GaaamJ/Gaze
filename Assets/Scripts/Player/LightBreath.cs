using UnityEngine;

namespace Gaze.Player
{
    // 빛 오브젝트의 크기를 사인파로 키웠다 줄여 숨쉬듯 보이게 함.
    // 라리스의 Light 2D 오브젝트에 붙임. 시작할 때의 Scale을 기준으로 흔듦.
    public class LightBreath : MonoBehaviour
    {
        [Tooltip("1초에 몇 번 숨쉬는지")]
        [SerializeField] float breathsPerSecond = 0.16f;
        [Tooltip("기준 Scale에서 커지고 작아지는 비율. 0.02 = ±2%")]
        [SerializeField] float amplitude = 0.02f;

        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        // 꺼질 때 기준 크기로 돌려 둠. 다시 켜졌을 때 중간 크기에서 멈춘 채 시작하지 않도록.
        void OnDisable()
        {
            transform.localScale = baseScale;
        }

        void Update()
        {
            float wave = Mathf.Sin(Time.time * breathsPerSecond * 2f * Mathf.PI);
            float scale = 1f + wave * amplitude;
            transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
        }
    }
}
