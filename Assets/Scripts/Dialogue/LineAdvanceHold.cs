#nullable enable

using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // 대사가 다 나온 뒤 holdSeconds 동안은 Z(진행 입력)를 받지 않음. 마지막 말이 화면에 머무는 여운을 주고, 연타로 대사를 놓치는 것을 막음.
    // 흐름: 대사 출력이 끝남(OnLineDisplayComplete) → holdSeconds 동안 Z 무시 → 그 뒤 Z는 LineAdvancer에 넘김.
    // 출력 중 Z로 대사를 한꺼번에 띄워도 그 순간부터 다시 기다림.
    //
    // Yarn의 LineAdvancer 입력 부품(LineAdvancerInput.InputActions)의 hurryUpLineAction은 비워 둬야 함.
    // 비우지 않으면 Z가 거기서도 처리되어 기다림이 무시됨. 선택지 입력(hurryUpOptionsAction)은 그대로 Yarn 부품이 맡음.
    // DialogueSystem 프리팹에서 LinePresenter의 eventHandlers에 등록함(출력 완료 시점을 받으려고).
    public class LineAdvanceHold : ActionMarkupHandler
    {
        [Tooltip("Z를 넘겨줄 Yarn LineAdvancer")]
        [SerializeField] LineAdvancer? lineAdvancer;
        [Tooltip("대사를 진행하는 입력. Player/Interact(Z)")]
        [SerializeField] InputActionReference? advanceAction;
        [Tooltip("대사가 다 나온 뒤 Z를 받지 않는 시간(초)")]
        [SerializeField, Min(0f)] float holdSeconds = 0.3f;

        // 대사 출력이 끝난 시각(unscaled). 출력 중이면 null.
        float? completedAt;

        // ContinueIndicator가 이 시간만큼 늦게 켜짐.
        public float HoldSeconds => holdSeconds;

        void OnEnable()
        {
            if (advanceAction == null)
                return;
            advanceAction.action.Enable();
            advanceAction.action.performed += OnAdvancePerformed;
        }

        void OnDisable()
        {
            if (advanceAction != null)
                advanceAction.action.performed -= OnAdvancePerformed;
        }

        // 대사 중이 아니면 LineAdvancer가 알아서 무시함(대사 상태가 아닐 때 hurry up은 아무 일도 안 함).
        // 시간 정지(timeScale 0)에도 재도록 unscaled 시간을 씀.
        void OnAdvancePerformed(InputAction.CallbackContext context)
        {
            if (lineAdvancer == null)
                return;
            if (completedAt.HasValue && Time.unscaledTime - completedAt.Value < holdSeconds)
                return;
            lineAdvancer.OnInputHurryUpLines();
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) => completedAt = null;

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) => YarnTask.CompletedTask;

        public override void OnLineDisplayComplete() => completedAt = Time.unscaledTime;

        public override void OnLineWillDismiss() => completedAt = null;
    }
}
