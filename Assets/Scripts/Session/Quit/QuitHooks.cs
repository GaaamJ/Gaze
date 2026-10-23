using System;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Session
{
    // 창 닫기(X·Alt+F4·작업 관리자 "작업 끝내기")는 Application.quitting으로 온다. 정상 종료 중이면 ExitRecorder가 무시한다.
    public static class QuitHooks
    {
        static bool installed;

        // 에디터 플레이 중지에도 종료 훅이 불려 사망으로 처리되므로 빌드에서만 건다. 에디터는 Tools > 창 닫기 시뮬레이션만 창 닫기로 본다.
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
        static void Install()
        {
            if (installed)
                return;
            installed = true;

            Application.quitting += OnQuitting;
#if UNITY_STANDALONE_WIN
            WindowsSessionEndHook.Install();
#endif
        }

        internal static void OnQuitting()
        {
#if UNITY_STANDALONE_WIN
            WindowsSessionEndHook.Uninstall();
#endif
            try
            {
                ExitRecorder.RecordWindowClose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
