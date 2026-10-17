using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using ProjectT.Area;
using ProjectT.Dialogue;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.UI;
using Object = UnityEngine.Object;

namespace ProjectT.Death
{
    public static class DeathSequence
    {
        const float DialogueStopTimeout = 2f;
        const float TeleportTimeout = 3f;

        public static bool IsActive { get; private set; }

        // 리로드된 씬의 SceneEntry가 Awake에서 읽고 비운다.
        public static bool ReloadedAfterDeath { get; private set; }

        // 에디트 모드 테스트에서는 씬을 불러올 수 없어 바꿔 끼운다.
        internal static Action ReloadScene = ReloadActiveScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void ResetState()
        {
            IsActive = false;
            ReloadedAfterDeath = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public static void Begin(string deathNodeId)
        {
            if (string.IsNullOrWhiteSpace(deathNodeId))
            {
                Debug.LogWarning("[DeathSequence] 사망 노드 이름이 비어 있어 사망시키지 않았다.");
                return;
            }

            if (IsActive)
                return;

            SceneEntry scene = Object.FindAnyObjectByType<SceneEntry>();
            if (scene == null)
            {
                Debug.LogError($"[DeathSequence] 씬에 SceneEntry가 없어 사망 '{deathNodeId}'를 처리하지 못했다.");
                return;
            }

            IsActive = true;
            InputLock inputLock = scene.InputLock;
            if (inputLock != null)
                inputLock.Lock(InputLockReason.Death);

            Debug.Log($"[DeathSequence] 사망 시작: {deathNodeId}", scene);
            Run(scene, inputLock, deathNodeId).Forget();
        }

        static async YarnTask Run(SceneEntry scene, InputLock inputLock, string deathNodeId)
        {
            bool reloading = false;
            ScreenFader fader = null;
            try
            {
                DialogueService dialogue = scene.Dialogue;
                if (dialogue != null)
                {
                    dialogue.RequestCancel();
                    if (!await dialogue.WaitUntilIdle(DialogueStopTimeout))
                        Debug.LogWarning("[DeathSequence] 진행 중인 대사가 제때 끝나지 않았다. 사망 노드가 거부될 수 있다.");
                }

                await WaitForTeleports();

                SaveProgress(deathNodeId);

                if (dialogue != null)
                {
                    DialogueResult result = await dialogue.Play(deathNodeId);
                    if (result == DialogueResult.Rejected)
                        Debug.LogWarning($"[DeathSequence] 사망 노드 '{deathNodeId}'를 재생하지 못했다. 그대로 리로드한다.");
                }
                else
                {
                    Debug.LogWarning($"[DeathSequence] DialogueService가 없어 사망 노드 '{deathNodeId}' 없이 리로드한다.");
                }

                fader = scene != null ? scene.FindOrCreateFader() : null;
                if (fader != null)
                {
                    IEnumerator fadeOut = fader.FadeOut();
                    while (fadeOut.MoveNext())
                        await YarnTask.Yield();
                }

                reloading = true;
                ReloadedAfterDeath = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
                ReloadScene();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                reloading = false;
            }
            finally
            {
                // 리로드에 들어가면 옛 씬 오브젝트는 건드리지 않는다. 잠금은 옛 씬과 함께 사라진다.
                if (!reloading)
                    Abort(inputLock, fader);
            }
        }

        static void Abort(InputLock inputLock, ScreenFader fader)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ReloadedAfterDeath = false;
            IsActive = false;
            if (inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            if (fader != null)
                fader.SetAlpha(0f);
        }

        // 리스폰 위치·퍼즐 클리어는 조사·풀이 시점에 이미 저장된다.
        // 사망 대사 도중 게임을 꺼도 시체가 남도록 대사보다 먼저 기록한다.
        static void SaveProgress(string deathNodeId)
        {
            DeathRecords.Add(deathNodeId);
        }

        static async YarnTask WaitForTeleports()
        {
            Teleport[] teleports = Object.FindObjectsByType<Teleport>(FindObjectsSortMode.None);
            float deadline = Time.realtimeSinceStartup + TeleportTimeout;
            while (AnyTransitioning(teleports))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogWarning("[DeathSequence] 텔레포트 전환이 제때 끝나지 않았다. 그대로 진행한다.");
                    return;
                }
                await YarnTask.Yield();
            }
        }

        static bool AnyTransitioning(Teleport[] teleports)
        {
            foreach (Teleport teleport in teleports)
            {
                if (teleport != null && teleport.IsTransitioning)
                    return true;
            }
            return false;
        }

        internal static bool ConsumeReload()
        {
            if (!ReloadedAfterDeath)
                return false;
            ReloadedAfterDeath = false;
            return true;
        }

        internal static void CompleteReload()
        {
            IsActive = false;
        }

        // SceneEntry가 없는 씬으로 리로드되면 사망 상태가 영구히 남지 않게 여기서 푼다.
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (!ReloadedAfterDeath)
                return;
            Debug.LogWarning($"[DeathSequence] 리로드된 씬 '{scene.name}'에 SceneEntry가 없어 사망 상태만 푼다.");
            ReloadedAfterDeath = false;
            IsActive = false;
        }

        static void ReloadActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // 빌드 설정에 없는 씬은 에디터에서도 SceneManager.LoadScene으로 불러올 수 없다.
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
