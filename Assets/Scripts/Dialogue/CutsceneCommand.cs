using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using Gaze.Death;

namespace Gaze.Dialogue
{
    // Yarn 커맨드 <<Cutscene id>>: Resources/Cutscenes/{id} 이미지를 대사창 뒤에 띄우고 대사는 그대로 진행함.
    // <<CutsceneEnd>> 또는 대화 종료 때 닫음.
    // 사망 대사가 끝날 때는 닫지 않음. 사망 페이드 아웃까지 컷신이 화면을 덮고, 리스폰 때 CoreEntry가 Hide로 닫음.
    [RequireComponent(typeof(DialogueRunner))]
    public class CutsceneCommand : MonoBehaviour
    {
        const string ShowCommandName = "Cutscene";
        const string EndCommandName = "CutsceneEnd";
        const string ResourceFolder = "Cutscenes/";

        [SerializeField] GameObject cutsceneRoot;
        [SerializeField] Image cutsceneImage;

        DialogueRunner runner;

        // 컷신이 떠 있으면 참. PortraitPresenter가 초상화를 띄울지 정할 때 봄.
        public bool IsShowing => cutsceneRoot != null && cutsceneRoot.activeSelf;

        // 컷신을 띄울 때마다 알림. PortraitPresenter가 받아 초상화를 숨김.
        public event System.Action Shown;

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
            runner.onDialogueComplete.AddListener(OnDialogueComplete);
        }

        void OnDisable()
        {
            runner.RemoveCommandHandler(ShowCommandName);
            runner.RemoveCommandHandler(EndCommandName);
            runner.onDialogueComplete.RemoveListener(OnDialogueComplete);
            Hide();
        }

        // 사망 중이면 닫지 않음. 닫으면 사망 페이드 아웃이 끝나기 전에 장면이 잠깐 보임.
        void OnDialogueComplete()
        {
            if (DeathSequence.IsActive)
                return;
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
            Shown?.Invoke();
        }

        // <<CutsceneEnd>>, 대화 종료, 리스폰(CoreEntry.ResetCore), 사망 중단(DeathSequence.Abort)에서 부름.
        public void Hide()
        {
            if (cutsceneRoot != null)
                cutsceneRoot.SetActive(false);
        }
    }
}
