using System;
using UnityEngine;
using Yarn.Unity;
using ProjectT.Session;

namespace ProjectT.Dialogue
{
    [RequireComponent(typeof(DialogueRunner))]
    public class DialogueService : MonoBehaviour
    {
        const string QuitNormallyCommandName = "QuitNormally";

        [SerializeField] InputLock inputLock;

        DialogueRunner runner;
        bool isPlaying;
        bool nodeCompleted;
        bool cancelRequested;

        // runner.Stop() 직후에도 Play의 정리가 끝나기 전까지는 다음 Play가 거부되므로 둘 다 본다.
        public bool IsIdle => !isPlaying && (runner == null || !runner.IsDialogueRunning);

        public event Action QuitNormallyRequested;

        void Awake()
        {
            runner = GetComponent<DialogueRunner>();

            if (inputLock == null)
                Debug.LogError("[DialogueService] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
        }

        void OnEnable()
        {
            runner.AddCommandHandler(QuitNormallyCommandName, OnQuitNormally);
        }

        void OnDisable()
        {
            runner.RemoveCommandHandler(QuitNormallyCommandName);
        }

        public bool HasNode(string nodeName)
        {
            return !string.IsNullOrEmpty(nodeName) && runner != null && runner.Dialogue.NodeExists(nodeName);
        }

        // 저장값만 지우면 다음 읽기 때 Yarn 프로젝트의 초기값이 쓰인다.
        public void ResetVariables()
        {
            if (runner != null && runner.VariableStorage != null)
                runner.VariableStorage.Clear();
        }

        public bool RequestCancel()
        {
            if (!isPlaying || cancelRequested || !runner.IsDialogueRunning)
                return false;

            cancelRequested = true;
            runner.Stop().Forget();
            return true;
        }

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
                // StartDialogue는 첫 내용을 띄우는 시점에 반환되므로 종료는 DialogueTask로 기다린다.
                await runner.StartDialogue(nodeName);
                await runner.DialogueTask;

                // DialogueTask가 끝난 직후에는 IsDialogueRunning이 아직 true라서 바로 다음 대화를 시작하면 거부된다.
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

        // 노드 끝이나 <<stop>>에서는 NodeComplete가 오지만, runner.Stop()·파괴로 끊기면 오지 않는다.
        void OnNodeComplete(string nodeName) => nodeCompleted = true;
    }
}
