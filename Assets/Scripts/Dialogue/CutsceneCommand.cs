using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Gaze.Dialogue
{
    // Yarn 커맨드 <<Cutscene id>>: Resources/Cutscenes/{id} 이미지를 대사창 뒤에 띄우고 대사는 그대로 진행함.
    // <<CutsceneEnd>> 또는 대화 종료 때 닫음.
    [RequireComponent(typeof(DialogueRunner))]
    public class CutsceneCommand : MonoBehaviour
    {
        const string ShowCommandName = "Cutscene";
        const string EndCommandName = "CutsceneEnd";
        const string ResourceFolder = "Cutscenes/";

        [SerializeField] GameObject cutsceneRoot;
        [SerializeField] Image cutsceneImage;

        DialogueRunner runner;

        void Awake()
        {
            runner = GetComponent<DialogueRunner>();
            cutsceneRoot.SetActive(false);
        }

        // 커맨드 핸들러는 러너에 이름으로 등록됨. 켜질 때 등록하고 꺼질 때 뺌.
        void OnEnable()
        {
            runner.AddCommandHandler<string>(ShowCommandName, Show);
            runner.AddCommandHandler(EndCommandName, Hide);
            runner.onDialogueComplete.AddListener(Hide);
        }

        void OnDisable()
        {
            runner.RemoveCommandHandler(ShowCommandName);
            runner.RemoveCommandHandler(EndCommandName);
            runner.onDialogueComplete.RemoveListener(Hide);
            Hide();
        }

        // <<Cutscene id>> 처리. 이미지가 없으면 경고만 남기고 대사는 계속 진행됨.
        void Show(string id)
        {
            Sprite sprite = Resources.Load<Sprite>(ResourceFolder + id);
            if (sprite == null)
            {
                Debug.LogWarning($"[Cutscene] '{ResourceFolder}{id}' 이미지가 없어 건너뛴다.", this);
                return;
            }

            cutsceneImage.sprite = sprite;
            cutsceneRoot.SetActive(true);
        }

        void Hide()
        {
            if (cutsceneRoot != null)
                cutsceneRoot.SetActive(false);
        }
    }
}
