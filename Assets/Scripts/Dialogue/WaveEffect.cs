#nullable enable

using UnityEngine;

namespace Gaze.Dialogue
{
    // 대사의 [wave]...[/wave] 구간 글자를 위아래로 출렁이게 함. 들뜨고 설레는 말.
    // 글자를 고르고 움직이는 틀은 MarkupMotionEffect가 맡고, 여기서는 출렁이는 거리만 정함.
    public class WaveEffect : MarkupMotionEffect
    {
        [Tooltip("출렁이는 높이. 글자 크기 대비 비율")]
        [SerializeField, Min(0f)] float amplitude = 0.12f;
        [Tooltip("출렁이는 빠르기(초당 라디안)")]
        [SerializeField, Min(0f)] float speed = 8f;
        [Tooltip("글자마다 어긋나는 위상(라디안). 클수록 파도가 촘촘함")]
        [SerializeField, Min(0f)] float phaseStep = 0.6f;

        protected override string MarkerName => "wave";

        protected override Vector3 Offset(int n, float height)
        {
            float y = Mathf.Sin(Time.unscaledTime * speed + n * phaseStep) * amplitude * height;
            return new Vector3(0f, y, 0f);
        }
    }
}
