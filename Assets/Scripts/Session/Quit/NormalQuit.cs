using System;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Session
{
    public static class NormalQuit
    {
        // 에디트 모드 테스트에서는 플레이를 멈출 수 없어 바꿔 끼운다.
        internal static Action StopApplication = Stop;

        // Yarn 커맨드 핸들러 안의 예외는 Yarn이 삼키므로 여기서 직접 로그로 남긴다.
        public static void Request()
        {
            bool quit;
            try
            {
                quit = ExitRecorder.BeginNormalQuit();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                quit = ExitRecorder.QuittingNormally;
            }

            if (!quit)
                return;

            Debug.Log("[NormalQuit] 정상 종료");
            try
            {
                StopApplication();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static void Stop()
        {
#if UNITY_EDITOR
            // 에디터에서는 Application.Quit이 동작하지 않는다.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
