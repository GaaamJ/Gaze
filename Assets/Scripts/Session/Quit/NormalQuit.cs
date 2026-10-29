using System;
using UnityEngine;
using Gaze.Save;

namespace Gaze.Session
{
    // 정상 종료(대사에서 요청하는 종료)를 맡음. 창 닫기 사망으로 기록되지 않게 막은 뒤 게임을 끔.
    // CoreEntry가 DialogueService.QuitNormallyRequested에 Request를 연결함.
    public static class NormalQuit
    {
        // 에디트 모드 테스트에서는 플레이를 멈출 수 없어 바꿔 끼움.
        internal static Action StopApplication = Stop;

        // 정상 종료 시작. 종료 상태 Normal 저장 → 게임 종료.
        // 이미 창 닫기나 정상 종료가 진행 중이면 아무것도 하지 않음.
        // Yarn 커맨드 핸들러 안의 예외는 Yarn이 삼키므로 여기서 직접 로그로 남김.
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
            // 에디터에서는 Application.Quit이 동작하지 않음.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
