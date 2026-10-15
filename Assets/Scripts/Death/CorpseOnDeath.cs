using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Death
{
    public class CorpseOnDeath : MonoBehaviour
    {
        [Tooltip("IDeathSource를 구현한 컴포넌트 (DeathInteractable, DeathZone, PasswordPuzzle)")]
        [SerializeField] MonoBehaviour source;
        [Tooltip("시체 모습으로 바꿀 스프라이트 렌더러")]
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite corpseSprite;

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
            if (target == null || corpseSprite == null)
            {
                Debug.LogWarning($"[CorpseOnDeath] '{name}': target 또는 corpseSprite가 비어 있다.", this);
                return;
            }

            if (DeathRecords.Has(death.DeathNodeId))
                target.sprite = corpseSprite;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (source != null && !(source is IDeathSource))
                Debug.LogWarning($"[CorpseOnDeath] '{name}': source '{source.GetType().Name}'는 IDeathSource가 아니다.", this);
        }
#endif
    }
}
