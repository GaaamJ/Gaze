#nullable enable

using UnityEngine;

namespace Gaze.Dialogue
{
    // 이 오브젝트의 CanvasGroup 알파를 다른 CanvasGroup의 알파에 맞춤.
    // OptionsPresenter의 LastLine(선택지 위에 남기는 직전 대사)에 붙임.
    // LastLine은 선택지 상자의 자식이 아니라서, 선택지가 끝나도 Yarn이 끄지 않고 다음 대사까지 남기 때문.
    [RequireComponent(typeof(CanvasGroup))]
    public class AlphaFollow : MonoBehaviour
    {
        [Tooltip("알파를 따라갈 CanvasGroup. LastLine이면 OptionsPresenter의 CanvasGroup")]
        [SerializeField] CanvasGroup? source;

        CanvasGroup own = null!;

        void Awake()
        {
            own = GetComponent<CanvasGroup>();
            if (source == null)
                Debug.LogError("[AlphaFollow] 'source' 참조가 비어 있다.", this);
        }

        // 켜질 때와 프레임 끝마다 맞춤. Yarn이 오브젝트를 켠 같은 프레임에 선택지 알파가 아직 0이어도 첫 프레임은 숨김.
        void OnEnable() => Follow();

        void LateUpdate() => Follow();

        void Follow()
        {
            if (source != null && own != null)
                own.alpha = source.alpha;
        }
    }
}
