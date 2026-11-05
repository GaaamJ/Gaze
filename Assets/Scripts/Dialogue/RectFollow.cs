#nullable enable

using UnityEngine;

namespace Gaze.Dialogue
{
    // 이 RectTransform의 위치와 크기를 다른 RectTransform에 늘 맞춤. 에디터에서도 따라감.
    // LastLine(선택지 위에 남기는 직전 대사)이 LinePresenter의 Text와 정확히 겹치게 하려고 씀.
    // 둘의 부모가 달라서 숫자를 손으로 맞추면 Text를 고칠 때마다 어긋남.
    // 같은 Canvas 안에 있고 둘 다 크기 배율이 1이어야 맞음.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RectFollow : MonoBehaviour
    {
        [Tooltip("따라갈 RectTransform. LastLine이면 LinePresenter 아래 Text")]
        [SerializeField] RectTransform? source;

        // 값이 다를 때만 씀. 같아도 쓰면 에디터가 매 프레임 바뀐 것으로 봄.
        // 앵커를 한 점으로 모아야 sizeDelta가 곧 크기가 됨.
        void LateUpdate()
        {
            if (source == null)
                return;

            var own = (RectTransform)transform;
            Vector2 anchor = own.anchorMin;
            if (own.anchorMax != anchor)
                own.anchorMax = anchor;
            if (own.pivot != source.pivot)
                own.pivot = source.pivot;
            Vector2 size = source.rect.size;
            if (own.sizeDelta != size)
                own.sizeDelta = size;
            if (own.position != source.position)
                own.position = source.position;
        }
    }
}
