using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    [CreateAssetMenu(fileName = "PasswordDoorPuzzleData", menuName = "ProjectT/Puzzle Data/Password Door")]
    public class PasswordDoorPuzzleData : ScriptableObject
    {
        public const int SymbolCount = 5;
        public const int AnswerCount = 2;

        [Tooltip("퍼즐 고유 이름. Object Data의 연결에서 가리킨다.")]
        [SerializeField] string id;

        [Tooltip("문 옆 문양 5개(반쪽을 합친 모양)")]
        [SerializeField] Sprite[] symbols = new Sprite[SymbolCount];

        [Tooltip("정답 문양 2개의 위 목록 번호(0부터). 순서는 상관없다.")]
        [SerializeField] int[] answerIndices = { 0, 1 };

        [Tooltip("오답일 때 발동할 사망 트리거 id")]
        [SerializeField] string wrongDeathTriggerId;

        public string Id => id;
        public IReadOnlyList<Sprite> Symbols => symbols;
        public IReadOnlyList<int> AnswerIndices => answerIndices;
        public string WrongDeathTriggerId => wrongDeathTriggerId;

        public List<string> CollectRuleWarnings()
        {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
                warnings.Add("id가 비어 있다.");
            if (symbols == null || symbols.Length != SymbolCount)
                warnings.Add($"문양은 {SymbolCount}개여야 한다.");
            if (answerIndices == null || answerIndices.Length != AnswerCount)
            {
                warnings.Add($"정답은 {AnswerCount}개여야 한다.");
            }
            else
            {
                if (answerIndices[0] == answerIndices[1])
                    warnings.Add("정답 두 개가 같은 문양을 가리킨다.");
                foreach (var index in answerIndices)
                {
                    if (index < 0 || index >= SymbolCount)
                        warnings.Add($"정답 번호 {index}가 0~{SymbolCount - 1} 밖이다.");
                }
            }
            if (string.IsNullOrWhiteSpace(wrongDeathTriggerId))
                warnings.Add("오답 사망 트리거 id가 비어 있다.");
            return warnings;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            foreach (var warning in CollectRuleWarnings())
                Debug.LogWarning($"[PasswordDoorPuzzleData] '{name}': {warning}", this);
        }
#endif
    }
}
