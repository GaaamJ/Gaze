#nullable enable

using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대화창 배경을 대사나 선택지가 보일 때만 보이게 함.
    // 배경은 서서히 켜지고 꺼지며, 켜지는 동안에는 대사의 첫 글자가 나오지 않고 기다림.
    // Dialogue Background 오브젝트에 붙이고, LinePresenter의 eventHandlers에도 등록함(첫 글자 대기용).
    //
    // 보일지는 글자 상자와 선택지 상자의 알파로 정함. 둘 다 0인 상태가 hideDelay 넘게 이어져야 꺼짐.
    // 선택지 ↔ 대사로 넘어갈 때 두 상자가 잠깐(몇 프레임) 모두 0이 되는데, 이때 배경이 깜박이지 않게 하려는 것.
    // `<<wait>>`처럼 오래 보여 줄 줄이 없으면 hideDelay 뒤에 꺼짐.
    [RequireComponent(typeof(CanvasGroup))]
    public class DialogueBackgroundFollow : ActionMarkupHandler
    {
        [Tooltip("이 중 하나라도 보이면 배경도 보임. LinePresenter, OptionsPresenter의 CanvasGroup")]
        [SerializeField] CanvasGroup[] sources = new CanvasGroup[0];
        [Tooltip("배경이 켜지는 데 걸리는 시간(초). 이 동안 대사의 첫 글자는 나오지 않음")]
        [SerializeField, Min(0f)] float fadeInSeconds = 0.2f;
        [Tooltip("배경이 꺼지는 데 걸리는 시간(초)")]
        [SerializeField, Min(0f)] float fadeOutSeconds = 0.15f;
        [Tooltip("두 상자가 모두 꺼진 채로 이 시간(초)이 지나야 배경이 꺼지기 시작함. 선택지 ↔ 대사 전환의 깜박임을 막음")]
        [SerializeField, Min(0f)] float hideDelay = 0.15f;

        CanvasGroup background = null!;
        float hiddenFor;
        bool wanted;

        // 배경이 완전히 켜졌으면 참. 대사의 첫 글자가 이걸 기다림.
        bool FullyShown => wanted && background.alpha >= 0.999f;

        void Awake()
        {
            background = GetComponent<CanvasGroup>();
            background.alpha = 0f;
        }

        // 프레임 끝에서 맞춤. Yarn이 같은 프레임에 알파를 바꿔도 마지막 값을 따라감.
        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            if (AnySourceVisible())
            {
                hiddenFor = 0f;
                wanted = true;
            }
            else
            {
                hiddenFor += dt;
                if (hiddenFor >= hideDelay)
                    wanted = false;
            }

            float target = wanted ? 1f : 0f;
            float seconds = wanted ? fadeInSeconds : fadeOutSeconds;
            background.alpha = seconds <= 0f
                ? target
                : Mathf.MoveTowards(background.alpha, target, dt / seconds);
        }

        bool AnySourceVisible()
        {
            foreach (CanvasGroup source in sources)
            {
                if (source != null && source.gameObject.activeInHierarchy && source.alpha > 0.001f)
                    return true;
            }
            return false;
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }

        // 대사의 첫 글자가 나오기 전에 배경이 다 켜질 때까지 기다림. 이미 켜져 있으면 바로 지나감.
        // 중간 글자는 기다리지 않음. 기다리는 도중 대사가 취소되면 멈춤.
        public override async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (currentCharacterIndex != 0)
                return;

            while (!FullyShown && !cancellationToken.IsCancellationRequested)
                await YarnTask.Yield();
        }

        public override void OnLineDisplayComplete() { }

        public override void OnLineWillDismiss() { }
    }
}
