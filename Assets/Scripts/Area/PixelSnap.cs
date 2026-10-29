using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaze.Area
{
    // 에디터에서 플레이 모드가 아닐 때만 월드 위치 x, y를 픽셀 격자에 맞춤. 빌드·플레이 중에는 아무것도 안 함.
    // 스프라이트 피벗이 픽셀 경계에 있어야 정확히 맞음(홀수 크기 스프라이트는 Center 피벗이 반 픽셀에 놓임).
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PixelSnap : MonoBehaviour
    {
        [Tooltip("격자 PPU")]
        [SerializeField, Min(1)] int pixelsPerUnit = 16;

#if UNITY_EDITOR
        const float Epsilon = 0.0001f;

        void Update()
        {
            if (Application.isPlaying)
                return;
            Snap();
        }

        // 위치를 가장 가까운 픽셀 격자로 반올림. 이미 맞아 있으면 Undo 기록 없이 넘어감.
        [ContextMenu("지금 픽셀에 맞추기")]
        void Snap()
        {
            Vector3 position = transform.position;
            float x = Mathf.Round(position.x * pixelsPerUnit) / pixelsPerUnit;
            float y = Mathf.Round(position.y * pixelsPerUnit) / pixelsPerUnit;
            if (Mathf.Abs(x - position.x) < Epsilon && Mathf.Abs(y - position.y) < Epsilon)
                return;

            Undo.RecordObject(transform, "Pixel Snap");
            transform.position = new Vector3(x, y, position.z);
        }
#endif
    }
}
