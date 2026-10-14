// 무엇: 사망의 입구 하나. 사망을 시작하면 입력을 잠그고(InputLockReason.Death) 화면을 검게 덮은 뒤 Started를 올린다.
//       잠금과 검은 화면은 씬을 다시 불러와도 End()까지 남는다.
// 어떻게 쓰나:
//   사망을 일으키는 쪽  → DeathSequence.Begin(deathData);
//   사망 연출(#52)      → DeathSequence.Started += data => { 사망 대사·게임 오버 ... ; DeathSequence.End(); };
// 주의:
//   - 사망 대사·컷씬·게임 오버는 여기서 하지 않는다. Started를 받은 쪽이 만든다.
//   - 대화 중에 Begin하면 대화를 끊고, 대화가 끝난 뒤에 Started를 올린다(사망이 이긴다). 그래서 Started 안에서 바로 대화를 재생할 수 있다.
//   - 검은 화면은 sortingOrder 200이라 ScreenFader(100)와 대화창(0) 위를 덮는다.
using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using ProjectT.Data;
using ProjectT.Dialogue;
using ProjectT.Session;
using Object = UnityEngine.Object;

namespace ProjectT.Death
{
    public static class DeathSequence
    {
        const int ScreenSortingOrder = 200;

        static int run;

        public static bool IsActive => InputLock.KeepDeathLock;

        public static event Action<DeathData> Started;

        internal static GameObject BlackScreen { get; private set; }

        public static void Begin(DeathData data)
        {
            if (IsActive)
                return;
            if (data == null)
            {
                Debug.LogError("[DeathSequence] DeathData가 비어 있어 사망을 시작하지 않았다.");
                return;
            }

            InputLock.KeepDeathLock = true;
            foreach (InputLock inputLock in Object.FindObjectsByType<InputLock>(FindObjectsSortMode.None))
                inputLock.Lock(InputLockReason.Death);
            BlackScreen = CreateScreen();

            DialogueService dialogue = Object.FindAnyObjectByType<DialogueService>();
            if (dialogue != null && dialogue.IsPlaying)
            {
                dialogue.RequestEnd();
                StartAfterDialogue(dialogue, data, ++run).Forget();
                return;
            }
            Started?.Invoke(data);
        }

        public static void End()
        {
            if (!IsActive)
                return;

            run++;
            InputLock.KeepDeathLock = false;
            foreach (InputLock inputLock in Object.FindObjectsByType<InputLock>(FindObjectsSortMode.None))
                inputLock.Unlock(InputLockReason.Death);
            if (BlackScreen != null)
                DestroyScreen(BlackScreen);
            BlackScreen = null;
        }

        static async YarnTask StartAfterDialogue(DialogueService dialogue, DeathData data, int startedRun)
        {
            while (dialogue != null && dialogue.IsPlaying)
                await YarnTask.Yield();
            // 기다리는 사이 End()가 불렸거나 사망이 다시 시작됐으면 이 사망의 Started는 버린다.
            if (startedRun == run)
                Started?.Invoke(data);
        }

        static GameObject CreateScreen()
        {
            var go = new GameObject("DeathScreen", typeof(Canvas), typeof(Image));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ScreenSortingOrder;
            go.GetComponent<Image>().color = Color.black;

            // DontDestroyOnLoad는 플레이 모드에서만 쓸 수 있다. 에디트 모드(테스트)에서는 열린 씬에 저장되지 않게만 한다.
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(go);
            else
                go.hideFlags = HideFlags.DontSave;
            return go;
        }

        static void DestroyScreen(GameObject screen)
        {
            if (Application.isPlaying)
                Object.Destroy(screen);
            else
                Object.DestroyImmediate(screen);
        }

        // 도메인 리로드를 끈 플레이 모드에서도 실행마다 사망 상태를 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            InputLock.KeepDeathLock = false;
            Started = null;
            BlackScreen = null;
            run++;
        }
    }
}
