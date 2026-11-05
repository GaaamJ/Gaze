#nullable enable

using UnityEngine;

namespace Gaze.Dialogue
{
    // 대화창 배경을 대사나 선택지가 보일 때만 보이게 함.
    // `<<wait>>`처럼 보여 줄 줄이 없는 동안에는 LinePresenter의 글자 상자만 꺼지고 배경은 남아서,
    // 글자 상자와 선택지 상자의 알파를 따라 배경의 알파를 맞춤.
    // Dialogue Background 오브젝트에 붙임.
    [RequireComponent(typeof(CanvasGroup))]
    public class DialogueBackgroundFollow : MonoBehaviour
    {
        [Tooltip("이 중 하나라도 보이면 배경도 보임. LinePresenter, OptionsPresenter의 CanvasGroup")]
        [SerializeField] CanvasGroup[] sources = new CanvasGroup[0];

        CanvasGroup background = null!;

        void Awake()
        {
            background = GetComponent<CanvasGroup>();
            Follow();
        }

        // 프레임 끝에서 맞춤. Yarn이 같은 프레임에 알파를 바꿔도 마지막 값을 따라감.
        void LateUpdate() => Follow();

        // 소스 알파 중 가장 큰 값을 배경 알파로 씀. 페이드 중에는 같이 변함.
        void Follow()
        {
            float alpha = 0f;
            foreach (CanvasGroup source in sources)
            {
                if (source != null && source.gameObject.activeInHierarchy)
                    alpha = Mathf.Max(alpha, source.alpha);
            }
            background.alpha = alpha;
        }
    }
}
