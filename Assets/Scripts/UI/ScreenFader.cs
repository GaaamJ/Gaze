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

        public float Alpha => image.color.a;

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

        public void SetAlpha(float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            image.enabled = alpha > 0f;
        }
    }
}
