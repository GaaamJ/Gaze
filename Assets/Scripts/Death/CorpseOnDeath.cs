using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Death
{
    public class CorpseOnDeath : MonoBehaviour
    {
        [Tooltip("IDeathSource를 구현한 컴포넌트 (DeathInteractable, DeathZone, PasswordPuzzle)")]
        [SerializeField] MonoBehaviour source;

        [Header("교체형")]
        [Tooltip("시체 모습으로 바꿀 스프라이트 렌더러")]
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite corpseSprite;

        [Header("필드형")]
        [Tooltip("사망 기록이 있으면 활성화할, 미리 배치해 둔 비활성 시체 오브젝트")]
        [SerializeField] GameObject corpseObject;

        bool HasSwap => target != null && corpseSprite != null;

        // 세이브는 GameSession.Begin(BeforeSceneLoad)에서 이미 불러와 있다.
        void Awake()
        {
            Apply();
        }

        internal void Apply()
        {
            if (!(source is IDeathSource death))
            {
                Debug.LogWarning($"[CorpseOnDeath] '{name}': source가 비었거나 IDeathSource가 아니다.", this);
                return;
            }
            if (!HasSwap && corpseObject == null)
            {
                Debug.LogWarning($"[CorpseOnDeath] '{name}': target 또는 corpseSprite가 비어 있고 corpseObject도 없다.", this);
                return;
            }

            if (!DeathRecords.Has(death.DeathNodeId))
                return;

            if (HasSwap)
                target.sprite = corpseSprite;
            if (corpseObject != null)
                corpseObject.SetActive(true);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (source != null && !(source is IDeathSource))
                Debug.LogWarning($"[CorpseOnDeath] '{name}': source '{source.GetType().Name}'는 IDeathSource가 아니다.", this);
            if ((target == null) != (corpseSprite == null))
                Debug.LogWarning($"[CorpseOnDeath] '{name}': 교체형은 target과 corpseSprite를 함께 지정해야 한다.", this);
        }
#endif
    }
}
