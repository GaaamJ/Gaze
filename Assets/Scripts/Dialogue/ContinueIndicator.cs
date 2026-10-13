#nullable enable

using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

namespace ProjectT.Dialogue
{
    public class ContinueIndicator : ActionMarkupHandler
    {
        [SerializeField] Graphic? graphic;
        [SerializeField, Min(0.05f)] float blinkInterval = 0.5f;

        float elapsed;

        public bool Waiting { get; private set; }

        void Awake()
        {
            if (graphic == null)
                Debug.LogError("[ContinueIndicator] 'graphic' 참조가 비어 있다.", this);
            Hide();
        }

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

        // 출력 중 Z(hurry up)로 끊겨도 타자기는 이 콜백을 부르므로 남은 글자가 한꺼번에 뜬 시점에 켜진다.
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
