using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    // 에셋에 숫자로 저장되므로 기존 값은 바꾸지 않고 새 값은 뒤에 추가한다.
    public enum GapKind
    {
        Hint = 0,
        Dangerous = 1
    }

    [CreateAssetMenu(fileName = "GapPuzzleData", menuName = "ProjectT/Puzzle Data/Gap")]
    public class GapPuzzleData : ScriptableObject
    {
        [Tooltip("퍼즐 고유 이름. Object Data의 연결에서 가리킨다.")]
        [SerializeField] string id;

        [Tooltip("힌트 틈 / 위험한 틈")]
        [SerializeField] GapKind kind;

        [Tooltip("플레이어 쪽 반쪽 문양")]
        [SerializeField] Sprite playerHalfSymbol;

        [Tooltip("라리스 쪽 반쪽 문양. 힌트 틈만")]
        [SerializeField] Sprite larisHalfSymbol;

        [Tooltip("조사 대사 노드")]
        [SerializeField] string dialogueNode;

        [Tooltip("연결된 사망 트리거 id. 위험한 틈만")]
        [SerializeField] string deathTriggerId;

        public string Id => id;
        public GapKind Kind => kind;
        public Sprite PlayerHalfSymbol => playerHalfSymbol;
        public Sprite LarisHalfSymbol => larisHalfSymbol;
        public string DialogueNode => dialogueNode;
        public string DeathTriggerId => deathTriggerId;

        public List<string> CollectRuleWarnings()
        {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
                warnings.Add("id가 비어 있다.");
            if (kind == GapKind.Dangerous && string.IsNullOrWhiteSpace(deathTriggerId))
                warnings.Add("위험한 틈인데 사망 트리거 id가 비어 있다.");
            if (kind == GapKind.Hint && !string.IsNullOrWhiteSpace(deathTriggerId))
                warnings.Add("힌트 틈에는 사망 트리거 id를 두지 않는다.");
            if (kind == GapKind.Dangerous && larisHalfSymbol != null)
                warnings.Add("위험한 틈에는 라리스 쪽 반쪽 문양을 두지 않는다.");
            return warnings;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            foreach (var warning in CollectRuleWarnings())
                Debug.LogWarning($"[GapPuzzleData] '{name}': {warning}", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(GapPuzzleData), dialogueNode);
        }
#endif
    }
}
