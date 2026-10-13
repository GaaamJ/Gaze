using UnityEngine;

namespace ProjectT.Data
{
    [CreateAssetMenu(fileName = "DeathSettings", menuName = "ProjectT/Death Settings")]
    public class DeathSettings : ScriptableObject
    {
        [Tooltip("장소 사망 트리거의 거부 영역 기본 크기. 트리거 콜라이더 바깥으로 이만큼 넓힌다(월드 단위, 사방).")]
        [SerializeField, Min(0.01f)] float refusalAreaMargin = 1f;

        public float RefusalAreaMargin => refusalAreaMargin;

        public Vector2 RefusalAreaSize(Vector2 triggerSize)
        {
            return triggerSize + Vector2.one * (refusalAreaMargin * 2f);
        }
    }
}
