#nullable enable

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yarn.Unity;

namespace ProjectT.Dialogue
{
    // 선택지가 떠 있는 동안 조작을 다룸. 선택 결과는 Yarn의 OptionsPresenter가 맡고, 이 클래스는 아래 두 가지만 함.
    // 1. 취소 입력(cancelAction)이 오면 대화를 취소함.
    // 2. 가로로 놓인 선택지를 ↑↓로 오갈 수 있게 연결함.
    // Yarn 러너의 프레젠터 목록에 OptionsPresenter와 함께 들어가며, 러너가 RunOptionsAsync를 부름.
    // DialogueSystem 프리팹의 러너 목록 순서는 LinePresenter, OptionsPresenter, OptionInputPresenter.
    public class OptionInputPresenter : DialoguePresenterBase
    {
        [SerializeField] InputActionReference? cancelAction;
        [SerializeField] DialogueService? dialogueService;
        [Tooltip("OptionItem이 생성되는 부모. OptionsPresenter의 CanvasGroup 오브젝트")]
        [SerializeField] Transform? optionsRoot;

        readonly List<Selectable> shownItems = new List<Selectable>();
        bool optionsRunning;
        CancellationToken optionsToken;

        // 선택지를 기다리는 중이고 아직 취소되지 않았으면 참.
        public bool OptionsShowing => optionsRunning && !optionsToken.IsCancellationRequested;

        void Awake()
        {
            if (cancelAction == null)
                Debug.LogError("[OptionInputPresenter] 'cancelAction' 참조가 비어 있다.", this);
            if (dialogueService == null)
                Debug.LogError("[OptionInputPresenter] 'dialogueService' 참조가 비어 있다.", this);
            if (optionsRoot == null)
                Debug.LogError("[OptionInputPresenter] 'optionsRoot' 참조가 비어 있다.", this);
            if (cancelAction == null || dialogueService == null || optionsRoot == null)
                enabled = false;
        }

        void OnEnable()
        {
            cancelAction!.action.performed += OnCancel;
            // Disable()하지 않는 이유는 PlayerMovement.OnEnable 참고.
            cancelAction.action.Enable();
        }

        void OnDisable()
        {
            cancelAction!.action.performed -= OnCancel;
        }

        // 선택지가 떠 있을 때만 DialogueService.RequestCancel을 부름. 취소를 요청했으면 true.
        public bool TryCancelDialogue()
        {
            if (!enabled || !OptionsShowing)
                return false;

            return dialogueService!.RequestCancel();
        }

        void OnCancel(InputAction.CallbackContext context) => TryCancelDialogue();

        // 선택지가 끝날 때까지 optionsRunning을 켜 두고 ↑↓ 연결을 건 뒤 null을 돌려줌.
        // 흐름: 선택 가능 여부 확인 → LinkUpDown → 선택지 취소 대기 → OptionItem 정리 대기 → null.
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            // 고를 수 있는 선택지가 없으면 OptionsPresenter는 바로 null을 돌려주므로, 여기서 기다리면 대화가 멈춤.
            if (!AnyAvailable(dialogueOptions))
                return null;

            // OptionsPresenter가 러너 목록에서 앞에 있어 이 시점에는 OptionItem이 이미 켜져 있음.
            if (optionsRoot != null)
                LinkUpDown(optionsRoot);

            optionsToken = cancellationToken.NextContentToken;
            optionsRunning = true;
            try
            {
                await YarnTask.WaitUntilCanceled(optionsToken);
                // OnDialogueCompleteAsync와 같은 Yarn 3.2.7 우회: OptionsPresenter가 OptionItem을 다 정리할 때까지 optionsRunning을 유지함.
                while (AnyItemActive())
                    await YarnTask.Yield();
            }
            finally
            {
                optionsRunning = false;
            }
            // 선택 결과는 OptionsPresenter가 돌려주고 러너는 null이 아닌 결과를 쓰므로, 이 프레젠터는 X 입력만 감시하고 null을 돌려줌.
            return null;
        }

        // root 아래 켜진 OptionItem의 내비게이션을 순환형 ↑↓로 연결함.
        // 선택지는 가로로 놓이지만 조작은 ↑↓만 쓰므로 표시 순서대로 ↑=이전, ↓=다음을 직접 이음.
        public void LinkUpDown(Transform root)
        {
            shownItems.Clear();
            foreach (Transform child in root)
            {
                if (child.gameObject.activeSelf && child.TryGetComponent(out OptionItem item))
                    shownItems.Add(item);
            }

            int count = shownItems.Count;
            for (int i = 0; i < count; i++)
            {
                shownItems[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = count > 1 ? shownItems[(i - 1 + count) % count] : null,
                    selectOnDown = count > 1 ? shownItems[(i + 1) % count] : null,
                };
            }
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) => YarnTask.CompletedTask;

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        // Yarn 3.2.7: 선택지 중 Stop()하면 러너가 선택지 처리를 마치기 전에 취소 상태를 지워 SetSelectedOption 예외가 남.
        // 선택지 처리(OptionsPresenter 정리 포함)가 끝날 때까지 대화 종료 완료를 늦춰 러너가 취소를 보게 함.
        public override async YarnTask OnDialogueCompleteAsync()
        {
            if (!optionsRunning)
                return;

            while (optionsRunning)
                await YarnTask.Yield();
            // optionsRunning이 꺼진 같은 프레임에는 러너가 아직 선택지 취소를 처리하기 전이라 한 프레임 더 늦춤.
            await YarnTask.Yield();
        }

        // root 아래 켜진 OptionItem이 하나라도 있으면 참.
        bool AnyItemActive()
        {
            if (optionsRoot == null)
                return false;

            foreach (Transform child in optionsRoot)
            {
                if (child.gameObject.activeSelf && child.TryGetComponent(out OptionItem _))
                    return true;
            }
            return false;
        }

        // 고를 수 있는(IsAvailable) 선택지가 하나라도 있으면 참.
        static bool AnyAvailable(DialogueOption[] options)
        {
            foreach (DialogueOption option in options)
            {
                if (option.IsAvailable)
                    return true;
            }
            return false;
        }
    }
}
