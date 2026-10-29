using System;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Session;

namespace ProjectT.Dialogue
{
    // DialogueRunner를 감싸 Yarn 노드 하나를 재생하고 결과(DialogueResult)를 돌려줌.
    // 재생 중 입력 잠금(InputLock)을 걸고, 취소·대기·변수 초기화·노드 존재 확인을 제공함.
    // DialogueInteractable, DeathSequence, DeathRefusalZone, CoreEntry(PlayDialogue)가 Play를 부름.
    [RequireComponent(typeof(DialogueRunner))]
    public class DialogueService : MonoBehaviour
    {
        const string QuitNormallyCommandName = "QuitNormally";

        [SerializeField] InputLock inputLock;

        DialogueRunner runner;
        bool isPlaying;
        bool nodeCompleted;
        bool cancelRequested;

        // 대화가 없고 새 Play를 받을 수 있으면 참.
        // runner.Stop() 직후에도 Play의 정리가 끝나기 전까지는 다음 Play가 거부되므로 둘 다 봄.
        public bool IsIdle => !isPlaying && (runner == null || !runner.IsDialogueRunning);

        // Yarn 커맨드 <<QuitNormally>>가 실행되면 발생함. CoreEntry가 NormalQuit.Request를 연결함.
        public event Action QuitNormallyRequested;

        void Awake()
        {
            runner = GetComponent<DialogueRunner>();

            if (inputLock == null)
                Debug.LogError("[DialogueService] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
        }

        // 커맨드 핸들러는 러너에 이름으로 등록됨. 켜질 때 등록하고 꺼질 때 뺌.
        void OnEnable()
        {
            runner.AddCommandHandler(QuitNormallyCommandName, OnQuitNormally);
        }

        void OnDisable()
        {
            runner.RemoveCommandHandler(QuitNormallyCommandName);
        }

        // 러너에 로드된 Yarn 프로젝트에 이 이름의 노드가 있는지 확인함.
        public bool HasNode(string nodeName)
        {
            return !string.IsNullOrEmpty(nodeName) && runner != null && runner.Dialogue.NodeExists(nodeName);
        }

        // Yarn 변수 저장값을 모두 지움. CoreEntry가 부름.
        // 저장값만 지우면 다음 읽기 때 Yarn 프로젝트의 초기값이 쓰임.
        public void ResetVariables()
        {
            if (runner != null && runner.VariableStorage != null)
                runner.VariableStorage.Clear();
        }

        // 재생 중인 대화를 끊음. 끊을 대화가 없거나 이미 요청했으면 false.
        // runner.Stop()은 기다리지 않고 띄움(.Forget()). 끊긴 Play는 DialogueResult.Cancelled를 돌려줌.
        // OptionInputPresenter(선택지 중 X 입력)와 DeathSequence가 부름.
        public bool RequestCancel()
        {
            if (!isPlaying || cancelRequested || !runner.IsDialogueRunning)
                return false;

            cancelRequested = true;
            runner.Stop().Forget();
            return true;
        }

        // IsIdle이 될 때까지 프레임마다 기다림. 시간 안에 끝나면 true, 초과하면 false.
        public async YarnTask<bool> WaitUntilIdle(float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!IsIdle)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    return false;
                await YarnTask.Yield();
            }
            return true;
        }

        // 노드 하나를 재생하고 대화가 완전히 끝나면 결과를 돌려줌.
        // 거부 검사(inputLock, IsIdle, HasNode) → 입력 잠금 → StartDialogue → 종료 대기 → 잠금 해제 → 결과 판정.
        // 결과: 시작 못 하면 Rejected, RequestCancel로 끊기면 Cancelled, 노드가 끝까지 가면 Completed, 그 외에 끊기면 Interrupted.
        public async YarnTask<DialogueResult> Play(string nodeName)
        {
            if (inputLock == null)
            {
                Debug.LogError($"[DialogueService] 'inputLock' 참조가 비어 있어 '{nodeName}' 재생을 거부했다.", this);
                return DialogueResult.Rejected;
            }

            if (!IsIdle)
            {
                Debug.LogWarning($"[DialogueService] 대화 중이라 '{nodeName}' 재생을 거부했다.", this);
                return DialogueResult.Rejected;
            }

            if (!HasNode(nodeName))
            {
                Debug.LogWarning($"[DialogueService] 노드 '{nodeName}'가 없다.", this);
                return DialogueResult.Rejected;
            }

            inputLock.Lock(InputLockReason.Dialogue);
            isPlaying = true;
            nodeCompleted = false;
            cancelRequested = false;
            runner.onNodeStart.AddListener(OnNodeStart);
            runner.onNodeComplete.AddListener(OnNodeComplete);

            try
            {
                // StartDialogue는 첫 내용을 띄우는 시점에 반환되므로 종료는 DialogueTask로 기다림.
                await runner.StartDialogue(nodeName);
                await runner.DialogueTask;

                // DialogueTask가 끝난 직후에는 IsDialogueRunning이 아직 true라서 바로 다음 대화를 시작하면 거부됨.
                while (runner.IsDialogueRunning)
                    await YarnTask.Yield();
            }
            finally
            {
                runner.onNodeStart.RemoveListener(OnNodeStart);
                runner.onNodeComplete.RemoveListener(OnNodeComplete);
                isPlaying = false;
                inputLock.Unlock(InputLockReason.Dialogue);
            }

            if (cancelRequested)
                return DialogueResult.Cancelled;
            return nodeCompleted ? DialogueResult.Completed : DialogueResult.Interrupted;
        }

        // <<QuitNormally>> 처리. 구독자가 없으면 로그만 남김.
        void OnQuitNormally()
        {
            if (QuitNormallyRequested == null)
            {
                Debug.Log("[DialogueService] <<QuitNormally>> 호출. 정상 종료 처리(#51)가 아직 연결되지 않았다.", this);
                return;
            }

            QuitNormallyRequested.Invoke();
        }

        void OnNodeStart(string nodeName) => nodeCompleted = false;

        // 노드 끝이나 <<stop>>에서는 NodeComplete가 오지만, runner.Stop()·파괴로 끊기면 오지 않음.
        void OnNodeComplete(string nodeName) => nodeCompleted = true;
    }
}
