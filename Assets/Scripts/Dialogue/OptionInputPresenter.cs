#nullable enable

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yarn.Unity;

namespace ProjectT.Dialogue
{
    public class OptionInputPresenter : DialoguePresenterBase
    {
        [SerializeField] InputActionReference? cancelAction;
        [SerializeField] DialogueService? dialogueService;
        [Tooltip("OptionItem이 생성되는 부모. OptionsPresenter의 CanvasGroup 오브젝트")]
        [SerializeField] Transform? optionsRoot;

        readonly List<Selectable> shownItems = new List<Selectable>();
        bool optionsRunning;
        CancellationToken optionsToken;

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

        public bool TryCancelDialogue()
        {
            if (!enabled || !OptionsShowing)
                return false;

            return dialogueService!.RequestCancel();
        }

        void OnCancel(InputAction.CallbackContext context) => TryCancelDialogue();

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            // 고를 수 있는 선택지가 없으면 OptionsPresenter는 바로 null을 돌려주므로, 여기서 기다리면 대화가 멈춘다.
            if (!AnyAvailable(dialogueOptions))
                return null;

            // OptionsPresenter가 러너 목록에서 앞에 있어 이 시점에는 OptionItem이 이미 켜져 있다.
            if (optionsRoot != null)
                LinkUpDown(optionsRoot);

            optionsToken = cancellationToken.NextContentToken;
            optionsRunning = true;
            try
            {
                await YarnTask.WaitUntilCanceled(optionsToken);
                // OnDialogueCompleteAsync와 같은 Yarn 3.2.7 우회: OptionsPresenter가 OptionItem을 다 정리할 때까지 optionsRunning을 유지한다.
                while (AnyItemActive())
                    await YarnTask.Yield();
            }
            finally
            {
                optionsRunning = false;
            }
            // 선택 결과는 OptionsPresenter가 돌려주고 러너는 null이 아닌 결과를 쓰므로, 이 프레젠터는 X 입력만 감시하고 null을 돌려준다.
            return null;
        }

        // 선택지는 가로로 놓이지만 조작은 ↑↓만 쓰므로 표시 순서대로 ↑=이전, ↓=다음을 직접 잇는다.
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

        // Yarn 3.2.7: 선택지 중 Stop()하면 러너가 선택지 처리를 마치기 전에 취소 상태를 지워 SetSelectedOption 예외가 난다.
        // 선택지 처리(OptionsPresenter 정리 포함)가 끝날 때까지 대화 종료 완료를 늦춰 러너가 취소를 보게 한다.
        public override async YarnTask OnDialogueCompleteAsync()
        {
            if (!optionsRunning)
                return;

            while (optionsRunning)
                await YarnTask.Yield();
            // optionsRunning이 꺼진 같은 프레임에는 러너가 아직 선택지 취소를 처리하기 전이라 한 프레임 더 늦춘다.
            await YarnTask.Yield();
        }

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
