#nullable enable

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대사의 [마커]...[/마커] 구간 글자를 매 프레임 움직이는 효과의 공통 부분. WaveEffect, ShakeEffect가 상속함.
    // 흐름: 대사 시작(OnLineDisplayBegin)에 움직일 글자를 정함 → LateUpdate마다 글자를 옮김 → 대사가 닫히면 LastLine으로 넘어감.
    // 선택지가 뜨면 OptionsPresenter가 같은 대사를 LastLine에 다시 적음. 그동안 같은 글자를 LastLine에서 계속 움직임.
    // 다음 대사가 시작되면 다시 LinePresenter의 글자로 돌아옴.
    // DialogueSystem 프리팹에서 LinePresenter의 eventHandlers에 등록함.
    public abstract class MarkupMotionEffect : ActionMarkupHandler
    {
        [Tooltip("선택지 위에 직전 대사를 보여 주는 TMP(LastLine). 비우면 대사가 닫힐 때 효과도 멈춤")]
        [SerializeField] TMP_Text? lastLineText;

        // 이 효과를 거는 Yarn 마커 이름. Ex. "wave"
        protected abstract string MarkerName { get; }

        // 움직일 글자의 TMP characterInfo index. 대사를 시작할 때 한 번 정함.
        readonly List<int> motionChars = new List<int>();
        TMP_Text? lineText;
        // 닫힌 대사의 글자. LastLine에 같은 글자가 적혀 있을 때만 거기서 움직임.
        string? dismissedText;

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        // 마커 구간을 글자 index로 바꿔 둠.
        // Yarn 속성의 Position은 <br> 같은 TMP 태그를 글자로 세서 TMP가 세는 글자 index와 어긋남.
        // TMP_CharacterInfo.index(원문 위치)로 이어 붙여 맞춤.
        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            motionChars.Clear();
            lineText = text;
            dismissedText = null;

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
                        motionChars.Add(i);
                }
            }
        }

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) => YarnTask.CompletedTask;

        public override void OnLineDisplayComplete() { }

        // 글자 목록은 지우지 않음. 선택지가 뜨면 LastLine에서 이어서 씀.
        public override void OnLineWillDismiss()
        {
            if (lineText != null)
                dismissedText = lineText.text;
            lineText = null;
        }

        // 타자기가 글자를 늘릴 때마다 TMP가 메시를 새로 만들므로, 매 프레임 원래 정점으로 되돌린 뒤 움직임을 더함.
        // 되돌리기는 TextMotion이 다른 효과와 나눠 맡음.
        void LateUpdate()
        {
            TMP_Text? target = CurrentTarget();
            if (target == null || motionChars.Count == 0)
                return;

            TMP_TextInfo info = TextMotion.BeginFrame(target);
            for (int n = 0; n < motionChars.Count; n++)
            {
                int i = motionChars[n];
                if (i >= info.characterCount)
                    break;

                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                    continue;

                // 글자 크기에 비례해 움직여서 `[big]` 글자는 더 크게 움직임.
                float height = character.topRight.y - character.bottomLeft.y;
                Vector3 offset = Offset(n, height);

                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                    vertices[character.vertexIndex + v] += offset;
            }
            TextMotion.Push(target);
        }

        // 대사가 떠 있으면 대사 글자, 닫힌 뒤 LastLine에 같은 대사가 보이면 LastLine.
        TMP_Text? CurrentTarget()
        {
            if (lineText != null)
                return lineText;
            if (lastLineText != null && dismissedText != null && lastLineText.isActiveAndEnabled && lastLineText.text == dismissedText)
                return lastLineText;
            return null;
        }

        // 이번 프레임에 n번째 효과 글자를 옮길 거리. height는 그 글자의 높이.
        // 시간 정지(timeScale 0)에도 움직이도록 unscaled 시간을 씀.
        protected abstract Vector3 Offset(int n, float height);
    }
}
