#nullable enable

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대사의 [wave]...[/wave] 구간 글자를 위아래로 출렁이게 함. 들뜨고 설레는 말.
    // 대사 단위의 시작·닫힘을 ActionMarkupHandler 콜백으로 받고, 출렁임은 LateUpdate에서 매 프레임 그림.
    // DialogueSystem 프리팹에서 LinePresenter의 eventHandlers에 등록함.
    public class WaveEffect : ActionMarkupHandler
    {
        const string MarkerName = "wave";

        [Tooltip("출렁이는 높이. 글자 크기 대비 비율")]
        [SerializeField, Min(0f)] float amplitude = 0.12f;
        [Tooltip("출렁이는 빠르기(초당 라디안)")]
        [SerializeField, Min(0f)] float speed = 8f;
        [Tooltip("글자마다 어긋나는 위상(라디안). 클수록 파도가 촘촘함")]
        [SerializeField, Min(0f)] float phaseStep = 0.6f;

        // 출렁일 글자의 TMP characterInfo index. 대사를 시작할 때 한 번 정함.
        readonly List<int> waveChars = new List<int>();
        TMP_Text? target;

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        // [wave] 구간을 글자 index로 바꿔 둠.
        // Yarn 속성의 Position은 line.Text 기준인데, 이 텍스트에는 <br> 같은 TMP 태그가 글자로 들어 있어서
        // TMP가 세는 글자 index와 어긋남. TMP_CharacterInfo.index(원문 위치)로 이어 붙여 맞춤.
        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            waveChars.Clear();
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
                        waveChars.Add(i);
                }
            }
        }

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) => YarnTask.CompletedTask;

        public override void OnLineDisplayComplete() { }

        // 다음 대사에 출렁임이 새지 않게 비움.
        public override void OnLineWillDismiss()
        {
            waveChars.Clear();
            target = null;
        }

        // 타자기가 글자를 늘릴 때마다 TMP가 메시를 새로 만들므로,
        // 매 프레임 원래 정점으로 되돌린 뒤(ForceMeshUpdate) 출렁임을 더함.
        // 시간 정지(timeScale 0)에도 움직이도록 unscaled 시간을 씀.
        void LateUpdate()
        {
            if (target == null || waveChars.Count == 0)
                return;

            target.ForceMeshUpdate();
            TMP_TextInfo info = target.textInfo;
            float time = Time.unscaledTime * speed;

            for (int n = 0; n < waveChars.Count; n++)
            {
                int i = waveChars[n];
                if (i >= info.characterCount)
                    break;

                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                    continue;

                // 글자 크기에 비례해 출렁여서 `[big]` 글자는 더 크게 출렁임.
                float height = character.topRight.y - character.bottomLeft.y;
                float offset = Mathf.Sin(time + n * phaseStep) * amplitude * height;

                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                    vertices[character.vertexIndex + v].y += offset;
            }

            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
                target.UpdateGeometry(info.meshInfo[m].mesh, m);
            }
        }
    }
}
