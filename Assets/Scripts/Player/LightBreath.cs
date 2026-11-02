using UnityEngine;

namespace Gaze.Player
{
    // 빛 오브젝트의 크기를 사인파로 키웠다 줄여 숨쉬듯 보이게 함.
    // 라리스의 Light 2D 오브젝트에 붙임. 시작할 때의 Scale을 기준으로 흔듦.
    // 픽셀 아트에 맞게 반지름은 stepPixels 단위로, 시간은 framesPerSecond 단위로 끊어 바꿈.
    public class LightBreath : MonoBehaviour
    {
        [Tooltip("1초에 몇 번 숨쉬는지")]
        [SerializeField] float breathsPerSecond = 0.16f;
        [Tooltip("기준 Scale에서 커지고 작아지는 비율. 0.02 = ±2%")]
        [SerializeField] float amplitude = 0.02f;
        [Tooltip("Scale 1일 때 빛의 반지름(게임 픽셀). SpotLight.png는 64")]
        [SerializeField] float radiusPixels = 64f;
        [Tooltip("반지름이 한 번에 바뀌는 픽셀 수. 0이면 끊지 않음")]
        [SerializeField] float stepPixels = 1f;
        [Tooltip("1초에 크기를 몇 번 바꾸는지. 0이면 매 프레임")]
        [SerializeField] float framesPerSecond = 10f;

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
            float time = framesPerSecond > 0f ? Mathf.Floor(Time.time * framesPerSecond) / framesPerSecond : Time.time;
            float wave = Mathf.Sin(time * breathsPerSecond * 2f * Mathf.PI);
            float offset = wave * amplitude;

            // 기준 Scale이 1이 아니면 1픽셀에 해당하는 Scale도 달라지므로 기준 반지름으로 나눔.
            float baseRadius = radiusPixels * baseScale.x;
            if (stepPixels > 0f && baseRadius > 0f)
            {
                float step = stepPixels / baseRadius;
                offset = Mathf.Round(offset / step) * step;
            }

            float scale = 1f + offset;
            transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
        }
    }
}
