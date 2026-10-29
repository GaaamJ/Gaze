using System;
using UnityEngine;
using Gaze.Save;

namespace Gaze.Session
{
    // 창 닫기(X·Alt+F4·작업 관리자 "작업 끝내기")를 감지해 ExitRecorder에 기록시킴.
    // 창 닫기는 Application.quitting으로 옴. 정상 종료 중이면 ExitRecorder가 무시함.
    // 윈도우 종료·로그오프는 Application.quitting이 오지 않아 WindowsSessionEndHook이 따로 맡음.
    public static class QuitHooks
    {
        static bool installed;

        // 종료 훅 설치. 빌드에서만 실행되고 한 번만 설치됨.
        // 에디터 플레이 중지에도 종료 훅이 불려 사망으로 처리되므로 빌드에서만 검. 에디터는 Tools > 창 닫기 시뮬레이션만 창 닫기로 봄.
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

        // 종료 시 창 메시지 훅을 되돌리고 창 닫기 사망을 기록함.
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
