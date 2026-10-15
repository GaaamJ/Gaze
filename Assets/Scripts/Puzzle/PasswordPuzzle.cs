using System;
using UnityEngine;
using ProjectT.Data;
using ProjectT.Death;
using ProjectT.Save;

namespace ProjectT.Puzzle
{
    public class PasswordPuzzle : MonoBehaviour, IDeathSource
    {
        [SerializeField] PasswordSymbol[] symbols = Array.Empty<PasswordSymbol>();
        [Tooltip("symbols 안에 있는 정답 문양.")]
        [SerializeField] PasswordSymbol[] answers = Array.Empty<PasswordSymbol>();
        [Tooltip("풀렸을 때 solvedPuzzleIds에 저장되는 id.")]
        [SerializeField] string puzzleId = "";

        [SerializeField] SpriteRenderer doorRenderer;
        [SerializeField] Sprite openSprite;
        [Tooltip("퍼즐이 풀리면 켜지는 오브젝트 (예: 문 너머 텔레포트).")]
        [SerializeField] GameObject[] activateOnOpen = Array.Empty<GameObject>();
        [Tooltip("오답 문양을 누르면 재생할 사망 노드 이름 (예: Death_PasswordWrong)")]
        [SerializeField] string wrongDeathNodeId = "";

        // 런타임 구독자는 없고 테스트가 오답 발생을 관찰하는 용도다.
        public event Action WrongSymbolPressed;

        public bool IsSolved { get; private set; }
        public string DeathNodeId => wrongDeathNodeId;

        void Awake()
        {
            RestoreFromSave();
        }

        void OnEnable() => SetSubscribed(true);

        void OnDisable() => SetSubscribed(false);

        void SetSubscribed(bool subscribed)
        {
            foreach (PasswordSymbol symbol in symbols)
            {
                if (symbol == null)
                    continue;
                if (subscribed)
                    symbol.Pressed += HandlePressed;
                else
                    symbol.Pressed -= HandlePressed;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrEmpty(puzzleId))
                Debug.LogWarning($"[PasswordPuzzle] puzzleId가 비어 있다: {name}", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(PasswordPuzzle), wrongDeathNodeId);
            foreach (PasswordSymbol answer in answers)
            {
                if (answer != null && Array.IndexOf(symbols, answer) < 0)
                    Debug.LogWarning($"[PasswordPuzzle] '{name}': 정답 문양 '{answer.name}'이 symbols에 없어 눌러도 반응하지 않아 퍼즐이 풀리지 않는다.", this);
            }
        }
#endif

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
                OnWrongSymbol();
                return;
            }

            symbol.SetLit(!symbol.IsLit);
            if (AllAnswersLit())
                Solve();
        }

        void OnWrongSymbol()
        {
            foreach (PasswordSymbol other in symbols)
            {
                if (other != null)
                    other.SetLit(false);
            }
            WrongSymbolPressed?.Invoke();
            DeathSequence.Begin(wrongDeathNodeId);
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
            SaveManager.AddUnique(SaveManager.Data.solvedPuzzleIds, puzzleId);
            ApplyOpened();
        }

        void ApplyOpened()
        {
            IsSolved = true;
            if (doorRenderer != null && openSprite != null)
                doorRenderer.sprite = openSprite;
            foreach (GameObject target in activateOnOpen)
            {
                if (target != null)
                    target.SetActive(true);
            }
        }
    }
}
