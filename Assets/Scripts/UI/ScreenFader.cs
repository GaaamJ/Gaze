using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gaze.UI
{
    // 화면 전체를 덮는 검은 이미지의 알파를 바꿔 페이드 아웃·인을 함.
    // FadeOut, FadeIn, HoldThenFadeIn은 IEnumerator라 호출하는 쪽이 MoveNext를 직접 돌려 기다림(await 불가).
    // CoreEntry(사망·리스폰 진입), DeathSequence, RoomSceneLoader(Room 이동)가 씀.
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Image image;
        [Tooltip("페이드 아웃·인 각각에 걸리는 시간(초)")]
        [SerializeField, Min(0f)] float duration = 0.2f;
        [Tooltip("완전히 검은 상태로 유지하는 시간(초)")]
        [SerializeField, Min(0f)] float holdDuration = 0.3f;

        // 화면이 어두워진 정도(0~1). 이미지 알파와 다를 수 있음(ToBlendAlpha 참고).
        public float Alpha { get; private set; }

        void Awake()
        {
            if (image == null)
            {
                Debug.LogError("[ScreenFader] 'image' 참조가 비어 있다.", this);
                return;
            }
            SetAlpha(0f);
        }

        // 씬의 ScreenFader를 찾고, 없으면 prefab으로 만들어 돌려줌. prefab도 없으면 경고 후 null.
        // 만든 페이더는 씬 루트에 두어 다음 전환·다른 컴포넌트가 FindAnyObjectByType으로 다시 쓰고, 씬과 함께 정리되게 함.
        public static ScreenFader FindOrCreate(ScreenFader prefab, Object context, bool warnIfMissing = true)
        {
            ScreenFader fader = FindAnyObjectByType<ScreenFader>();
            if (fader != null)
                return fader;
            if (prefab != null)
                return Instantiate(prefab);
            if (warnIfMissing)
                Debug.LogWarning($"[ScreenFader] '{(context != null ? context.name : "")}': 씬에 ScreenFader가 없고 'faderPrefab'도 비어 있어 페이드 없이 진행한다.", context);
            return null;
        }

        // FadeOut: 검게 덮음. FadeIn: 걷어 냄. 둘 다 duration 동안 진행됨.
        public IEnumerator FadeOut() => FadeTo(1f);
        public IEnumerator FadeIn() => FadeTo(0f);

        // 검은 상태를 holdDuration 동안 유지한 뒤 FadeIn. RoomSceneLoader가 Room 이동 끝에 씀.
        public IEnumerator HoldThenFadeIn()
        {
            // duration이 0이면 연출 전체를 즉시로 봄. 시간이 흐르지 않는 테스트에서 유지 대기에 멈추지 않게 하기 위함.
            if (duration > 0f)
            {
                for (float t = 0f; t < holdDuration; t += Time.unscaledDeltaTime)
                    yield return null;
            }
            IEnumerator fadeIn = FadeIn();
            while (fadeIn.MoveNext())
                yield return fadeIn.Current;
        }

        // 현재 Alpha에서 target까지 duration 동안 선형으로 바꿈. 시간 정지에도 진행되도록 unscaled 시간을 씀.
        IEnumerator FadeTo(float target)
        {
            float start = Alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            SetAlpha(target);
        }

        // 어두운 정도를 즉시 정함. 0이면 이미지를 꺼 둠.
        public void SetAlpha(float value)
        {
            Alpha = Mathf.Clamp01(value);
            Color color = image.color;
            color.a = ToBlendAlpha(Alpha);
            image.color = color;
            image.enabled = Alpha > 0f;
        }

        // Linear 색 공간에서는 검은 알파가 선형값에 섞여 밝은 UI(대화창 테두리 등)가 중간 페이드에도 밝게 남으므로, 화면 밝기가 alpha만큼 줄도록 보정함.
        static float ToBlendAlpha(float darkness)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                return darkness;
            return 1f - Mathf.GammaToLinearSpace(1f - darkness);
        }
    }
}
