using System;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Puzzle
{
    public class PasswordPuzzle : MonoBehaviour
    {
        [SerializeField] PasswordSymbol[] symbols = Array.Empty<PasswordSymbol>();
        [Tooltip("symbols 안에 있는 정답 문양.")]
        [SerializeField] PasswordSymbol[] answers = Array.Empty<PasswordSymbol>();
        [Tooltip("풀렸을 때 solvedPuzzleIds에 저장되는 id.")]
        [SerializeField] string puzzleId = "";

        [SerializeField] SpriteRenderer doorRenderer;
        [SerializeField] Sprite openSprite;
        [Tooltip("문이 닫혀 있는 동안 길을 막는 콜라이더.")]
        [SerializeField] Collider2D doorCollider;

        public event Action WrongSymbolPressed;

        public bool IsSolved { get; private set; }
        public string PuzzleId => puzzleId;

        void Awake()
        {
            RestoreFromSave();
        }

        void OnEnable()
        {
            foreach (PasswordSymbol symbol in symbols)
            {
                if (symbol != null)
                    symbol.Pressed += HandlePressed;
            }
        }

        void OnDisable()
        {
            foreach (PasswordSymbol symbol in symbols)
            {
                if (symbol != null)
                    symbol.Pressed -= HandlePressed;
            }
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(puzzleId))
                Debug.LogWarning($"[PasswordPuzzle] puzzleId가 비어 있다: {name}", this);
        }

        internal void RestoreFromSave()
        {
            if (string.IsNullOrEmpty(puzzleId) || !SaveManager.Data.solvedPuzzleIds.Contains(puzzleId))
                return;

            foreach (PasswordSymbol answer in answers)
            {
                if (answer != null)
                    answer.SetLit(true);
            }
            ApplyOpened();
        }

        internal void HandlePressed(PasswordSymbol symbol)
        {
            if (IsSolved || symbol == null)
                return;

            if (Array.IndexOf(answers, symbol) < 0)
            {
                foreach (PasswordSymbol other in symbols)
                {
                    if (other != null)
                        other.SetLit(false);
                }
                WrongSymbolPressed?.Invoke();
                Debug.Log("[PasswordPuzzle] 오답 문양: 사망 발동 지점 (#52 연결 전)", this);
                return;
            }

            symbol.SetLit(!symbol.IsLit);
            if (AllAnswersLit())
                Solve();
        }

        bool AllAnswersLit()
        {
            if (answers.Length == 0)
                return false;
            foreach (PasswordSymbol answer in answers)
            {
                if (answer == null || !answer.IsLit)
                    return false;
            }
            return true;
        }

        void Solve()
        {
            if (!SaveManager.Data.solvedPuzzleIds.Contains(puzzleId))
                SaveManager.Data.solvedPuzzleIds.Add(puzzleId);
            if (!SaveManager.Save())
                Debug.LogWarning($"[PasswordPuzzle] 퍼즐 해결 저장에 실패했다: {puzzleId}", this);
            ApplyOpened();
        }

        void ApplyOpened()
        {
            IsSolved = true;
            if (doorRenderer != null && openSprite != null)
                doorRenderer.sprite = openSprite;
            if (doorCollider != null)
                doorCollider.enabled = false;
        }
    }
}
