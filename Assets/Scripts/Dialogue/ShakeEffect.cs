#nullable enable

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대사의 [shake]...[/shake] 구간 글자를 덜덜 떨게 함. 아픔, 두려움, 추위.
    // 글자마다 따로, 일정 간격으로 끊어서 자리를 바꿔 매끄럽지 않고 떨리는 느낌을 냄.
    // DialogueSystem 프리팹에서 LinePresenter의 eventHandlers에 등록함.
    public class ShakeEffect : ActionMarkupHandler
    {
        const string MarkerName = "shake";

        [Tooltip("떨리는 폭. 글자 크기 대비 비율")]
        [SerializeField, Min(0f)] float amplitude = 0.06f;
        [Tooltip("자리를 바꾸는 간격(초). 작을수록 빠르게 떪")]
        [SerializeField, Min(0.01f)] float interval = 0.05f;

        // 떨릴 글자의 TMP characterInfo index. 대사를 시작할 때 한 번 정함.
        readonly List<int> shakeChars = new List<int>();
        TMP_Text? target;

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        // [shake] 구간을 글자 index로 바꿔 둠.
        // Yarn 속성의 Position은 <br> 같은 TMP 태그를 글자로 세므로 TMP_CharacterInfo.index(원문 위치)로 맞춤.
        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            shakeChars.Clear();
            target = text;

            TMP_TextInfo info = text.GetTextInfo(line.Text);
            foreach (MarkupAttribute attribute in line.Attributes)
            {
                if (attribute.Name != MarkerName)
                    continue;

                int start = attribute.Position;
                int end = attribute.Position + attribute.Length;
                for (int i = 0; i < info.characterCount; i++)
                {
                    int sourceIndex = info.characterInfo[i].index;
                    if (sourceIndex >= start && sourceIndex < end)
                        shakeChars.Add(i);
                }
            }
        }

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) => YarnTask.CompletedTask;

        public override void OnLineDisplayComplete() { }

        // 다음 대사에 떨림이 새지 않게 비움.
        public override void OnLineWillDismiss()
        {
            shakeChars.Clear();
            target = null;
        }

        // 시간 정지(timeScale 0)에도 떨리도록 unscaled 시간을 씀.
        void LateUpdate()
        {
            if (target == null || shakeChars.Count == 0)
                return;

            TMP_TextInfo info = TextMotion.BeginFrame(target);
            int step = Mathf.FloorToInt(Time.unscaledTime / interval);

            for (int n = 0; n < shakeChars.Count; n++)
            {
                int i = shakeChars[n];
                if (i >= info.characterCount)
                    break;

                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                    continue;

                float height = character.topRight.y - character.bottomLeft.y;
                Vector3 offset = new Vector3(Noise(n, step), Noise(n + 7919, step), 0f) * (amplitude * height);

                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                    vertices[character.vertexIndex + v] += offset;
            }

            TextMotion.Push(target);
        }

        // 글자와 시간 칸마다 정해진 -1 ~ 1 값. 같은 칸 안에서는 그대로라 한 칸 동안 자리가 멈춤.
        static float Noise(int seed, int step)
        {
            float x = Mathf.Sin(seed * 12.9898f + step * 78.233f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }
    }
}
