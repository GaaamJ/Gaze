#nullable enable

using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대사 출력이 끝나 입력을 기다리는 동안 graphic을 깜박이는 표시.
    // Yarn의 ActionMarkupHandler 콜백으로 대사 단위의 시작·완료·닫힘을 받음.
    // DialogueSystem 프리팹에서 LinePresenter의 eventHandlers에 등록되어 있음.
    public class ContinueIndicator : ActionMarkupHandler
    {
        [SerializeField] Graphic? graphic;
        [SerializeField, Min(0.05f)] float blinkInterval = 0.5f;

        float elapsed;

        // 대사 출력이 끝나 표시가 깜박이는 중이면 참.
        public bool Waiting { get; private set; }

        void Awake()
        {
            if (graphic == null)
                Debug.LogError("[ContinueIndicator] 'graphic' 참조가 비어 있다.", this);
            Hide();
        }

        // 기다리는 동안 blinkInterval마다 graphic을 켜고 끔. 시간 정지(timeScale 0)에도 깜박이도록 unscaled 시간을 씀.
        void Update()
        {
            if (!Waiting || graphic == null)
                return;

            elapsed += Time.unscaledDeltaTime;
            graphic.enabled = Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0;
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) => Hide();

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) => Hide();

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) => YarnTask.CompletedTask;

        // 대사 출력이 끝나면 표시를 켬.
        // 출력 중 Z(hurry up)로 끊겨도 타자기는 이 콜백을 부르므로 남은 글자가 한꺼번에 뜬 시점에 켜짐.
        public override void OnLineDisplayComplete()
        {
            Waiting = true;
            elapsed = 0f;
            if (graphic != null)
                graphic.enabled = true;
        }

        public override void OnLineWillDismiss() => Hide();

        void Hide()
        {
            Waiting = false;
            elapsed = 0f;
            if (graphic != null)
                graphic.enabled = false;
        }
    }
}
