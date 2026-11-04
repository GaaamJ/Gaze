using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 타자기로 대사를 찍을 때 줄바꿈(<br>)과 문장부호 뒤에서 자동으로 잠깐 멈춤.
    // 대사마다 [pause]를 쓰지 않아도 숨 쉬는 자리가 생기게 함. 직접 쓴 [pause]는 그대로 더해짐.
    // LinePresenter의 eventHandlers 목록에 들어가며, 타자기가 글자마다 OnCharacterWillAppear를 부름.
    public class PunctuationPause : ActionMarkupHandler
    {
        [Tooltip("<br> 줄바꿈 뒤에 멈추는 시간(밀리초)")]
        [SerializeField] float lineBreakMs = 200f;
        [Tooltip(". ! ? … ~ 뒤에 멈추는 시간(밀리초)")]
        [SerializeField] float sentenceMs = 150f;
        [Tooltip(", 뒤에 멈추는 시간(밀리초)")]
        [SerializeField] float commaMs = 80f;

        // 글자 index → 그 글자가 나오기 전에 멈출 시간(밀리초).
        readonly Dictionary<int, float> pauses = new Dictionary<int, float>();

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        // 대사를 훑어 멈출 자리를 미리 정함.
        // 문장부호·줄바꿈을 만나면 멈춤을 쌓아 두고, 다음에 오는 글자(공백·부호가 아닌 것) 앞에서 멈춤.
        // 그래서 "..."나 "?!"처럼 부호가 이어지면 마지막 부호 뒤에서 한 번만 멈추고, 여러 이유가 겹치면 긴 쪽을 씀.
        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            pauses.Clear();

            // 타자기가 세는 글자 index와 같은 기준(TMP의 characterInfo)으로 봄. <br>은 여기서 '\n' 한 글자가 됨.
            TMP_TextInfo info = text.GetTextInfo(line.Text);
            float pending = 0f;
            for (int i = 0; i < info.characterCount; i++)
            {
                char c = info.characterInfo[i].character;

                if (pending > 0f && !char.IsWhiteSpace(c) && PauseFor(c) == 0f)
                {
                    pauses[i] = pending;
                    pending = 0f;
                }

                pending = Mathf.Max(pending, PauseFor(c));
            }
        }

        public override async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (pauses.TryGetValue(currentCharacterIndex, out float ms))
                await YarnTask.Delay(System.TimeSpan.FromMilliseconds(ms), cancellationToken).SuppressCancellationThrow();
        }

        public override void OnLineDisplayComplete() => pauses.Clear();

        public override void OnLineWillDismiss() { }

        // 이 글자 뒤에 멈출 시간. 멈추지 않는 글자면 0.
        float PauseFor(char c)
        {
            switch (c)
            {
                case '\n':
                    return lineBreakMs;
                case '.':
                case '!':
                case '?':
                case '…':
                case '~':
                    return sentenceMs;
                case ',':
                    return commaMs;
                default:
                    return 0f;
            }
        }
    }
}
