using System;
using TMPro;
using UnityEngine;
using Yarn.Unity;
using Gaze.Save;

namespace Gaze.Dialogue
{
    // Yarn 커맨드 <<UsePcUserName>>, <<AskPlayerName>>: 플레이어 이름을 정하고 바로 저장함(PlayerName).
    // <<AskPlayerName>>은 입력창을 띄우고, 이름을 확정할 때까지 대사를 멈춤.
    // DialogueSystem 프리팹의 러너 오브젝트에 붙음.
    [RequireComponent(typeof(DialogueRunner))]
    public class PlayerNameCommand : MonoBehaviour
    {
        const string UsePcUserNameCommandName = "UsePcUserName";
        const string AskCommandName = "AskPlayerName";

        [SerializeField] GameObject inputRoot;
        [SerializeField] TMP_InputField inputField;

        DialogueRunner runner;
        string submitted;

        void Awake()
        {
            runner = GetComponent<DialogueRunner>();
            inputRoot.SetActive(false);
        }

        // 커맨드 핸들러는 러너에 이름으로 등록됨. 켜질 때 등록하고 꺼질 때 뺌.
        void OnEnable()
        {
            runner.AddCommandHandler(UsePcUserNameCommandName, UsePcUserName);
            runner.AddCommandHandler(AskCommandName, (Func<YarnTask>)Ask);
            inputField.onSubmit.AddListener(OnSubmit);
        }

        void OnDisable()
        {
            runner.RemoveCommandHandler(UsePcUserNameCommandName);
            runner.RemoveCommandHandler(AskCommandName);
            inputField.onSubmit.RemoveListener(OnSubmit);
            Hide();
        }

        void UsePcUserName() => PlayerName.Set(PlayerNameFunctions.PcUserName());

        // 입력창 표시 → 빈 이름이 아닌 확정까지 대기 → 저장 → 닫기.
        // 커맨드가 YarnTask를 돌려주면 러너가 끝날 때까지 다음 줄로 넘어가지 않음.
        // 기다리는 중 대화가 끊기면(사망 등) 저장하지 않고 닫음.
        async YarnTask Ask()
        {
            submitted = null;
            inputField.text = "";
            inputRoot.SetActive(true);

            try
            {
                while (submitted == null && runner.IsDialogueRunning)
                {
                    // 다른 곳을 클릭해 포커스를 잃으면 키 입력이 들어오지 않으므로 다시 잡음.
                    if (!inputField.isFocused)
                        inputField.ActivateInputField();
                    await YarnTask.Yield();
                }

                if (submitted != null)
                    PlayerName.Set(submitted);
            }
            finally
            {
                Hide();
            }
        }

        // Enter로 확정. 공백뿐이면 무시하고 입력을 이어 감.
        void OnSubmit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                submitted = text;
        }

        void Hide()
        {
            if (inputRoot != null)
                inputRoot.SetActive(false);
        }
    }
}
