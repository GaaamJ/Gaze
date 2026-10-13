using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectT.UI
{
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Image image;
        [Tooltip("페이드 아웃·인 각각에 걸리는 시간(초)")]
        [SerializeField, Min(0f)] float duration = 0.3f;

        float alpha;

        // 화면이 어두워진 정도(0~1). 이미지 알파와 다를 수 있다.
        public float Alpha => alpha;

        void Awake()
        {
            if (image == null)
            {
                Debug.LogError("[ScreenFader] 'image' 참조가 비어 있다.", this);
                return;
            }
            SetAlpha(0f);
        }

        public IEnumerator FadeOut() => FadeTo(1f);
        public IEnumerator FadeIn() => FadeTo(0f);

        public IEnumerator FadeTo(float target)
        {
            float start = Alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            SetAlpha(target);
        }

        public void SetAlpha(float value)
        {
            alpha = Mathf.Clamp01(value);
            Color color = image.color;
            color.a = ToBlendAlpha(alpha);
            image.color = color;
            image.enabled = alpha > 0f;
        }

        // Linear 색 공간에서는 검은 알파가 선형값에 섞여 밝은 UI(대화창 테두리 등)가 중간 페이드에도 밝게 남으므로, 화면 밝기가 alpha만큼 줄도록 보정한다.
        static float ToBlendAlpha(float darkness)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                return darkness;
            return 1f - Mathf.GammaToLinearSpace(1f - darkness);
        }
    }
}
