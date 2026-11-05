#nullable enable

using TMPro;
using UnityEngine;

namespace Gaze.Dialogue
{
    // 글자 정점을 움직이는 효과(WaveEffect, ShakeEffect)가 같은 대사에 함께 쓰일 때 메시를 나눠 쓰게 함.
    // 효과마다 LateUpdate에서 BeginFrame → 정점 이동 → Push 순으로 부름.
    // 원래 정점으로 되돌리는 일(ForceMeshUpdate)은 한 프레임에 한 번만 해서, 먼저 돈 효과의 움직임이 지워지지 않음.
    public static class TextMotion
    {
        static TMP_Text? resetTarget;
        static int resetFrame = -1;

        // 이번 프레임에 이 텍스트를 아직 안 되돌렸으면 원래 정점으로 되돌림.
        public static TMP_TextInfo BeginFrame(TMP_Text text)
        {
            if (resetTarget != text || resetFrame != Time.frameCount)
            {
                text.ForceMeshUpdate();
                resetTarget = text;
                resetFrame = Time.frameCount;
            }
            return text.textInfo;
        }

        // 바꾼 정점을 메시에 올림. 효과마다 불러도 됨: 정점 배열을 함께 쓰므로 마지막 Push에 모든 효과가 담김.
        public static void Push(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
                text.UpdateGeometry(info.meshInfo[m].mesh, m);
            }
        }
    }
}
