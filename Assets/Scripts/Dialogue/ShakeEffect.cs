#nullable enable

using UnityEngine;

namespace Gaze.Dialogue
{
    // 대사의 [shake]...[/shake] 구간 글자를 덜덜 떨게 함. 아픔, 두려움, 추위.
    // 글자마다 따로, 일정 간격으로 끊어서 자리를 바꿔 매끄럽지 않고 떨리는 느낌을 냄.
    // 글자를 고르고 움직이는 틀은 MarkupMotionEffect가 맡고, 여기서는 떨리는 거리만 정함.
    public class ShakeEffect : MarkupMotionEffect
    {
        [Tooltip("떨리는 폭. 글자 크기 대비 비율")]
        [SerializeField, Min(0f)] float amplitude = 0.06f;
        [Tooltip("자리를 바꾸는 간격(초). 작을수록 빠르게 떪")]
        [SerializeField, Min(0.01f)] float interval = 0.05f;

        protected override string MarkerName => "shake";

        protected override Vector3 Offset(int n, float height)
        {
            int step = Mathf.FloorToInt(Time.unscaledTime / interval);
            return new Vector3(Noise(n, step), Noise(n + 7919, step), 0f) * (amplitude * height);
        }

        // 글자와 시간 칸마다 정해진 -1 ~ 1 값. 같은 칸 안에서는 그대로라 한 칸 동안 자리가 멈춤.
        static float Noise(int seed, int step)
        {
            float x = Mathf.Sin(seed * 12.9898f + step * 78.233f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }
    }
}
