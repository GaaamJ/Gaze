using ProjectT.Core.GameFolder;
using UnityEngine;

namespace ProjectT.Save
{
    // 창 닫기(빌드 종료 훅·WM_ENDSESSION·에디터 시뮬레이션)와 정상 종료가 같은 플래그를 보고 서로를 막는다.
    public static class ExitRecorder
    {
        public const string WindowCloseDeathNodeId = "Death_CloseWindow";

        public static bool WindowCloseRecorded { get; private set; }
        public static bool QuittingNormally { get; private set; }

        // 도메인 리로드를 끈 플레이 모드에서도 실행마다 플래그를 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetState()
        {
            WindowCloseRecorded = false;
            QuittingNormally = false;
        }

        public static void RecordWindowClose()
        {
            if (WindowCloseRecorded || QuittingNormally)
                return;
            WindowCloseRecorded = true;

            SaveData data = SaveManager.Data;
            data.exitState = ExitState.DeathPending;
            if (!data.diedNodeIds.Contains(WindowCloseDeathNodeId))
                data.diedNodeIds.Add(WindowCloseDeathNodeId);

            if (!SaveManager.Save())
            {
                Debug.LogError("[ExitRecorder] 창 닫기 사망을 저장하지 못해 시체 파일을 만들지 않았다.");
                return;
            }

            if (CorpseFiles.TryCreate(SaveManager.Paths, out string corpse, out string error))
                Debug.Log($"[ExitRecorder] 창 닫기 사망 저장, 시체 파일 생성: {corpse}");
            else
                Debug.LogError($"[ExitRecorder] 창 닫기 사망은 저장했지만 시체 파일을 만들지 못했다.\n{error}");
        }

        // 저장에 실패해도 종료는 진행한다. 디스크의 종료 상태는 시작 때 None으로 비워 두었으므로 다음 실행은 튕김으로 본다.
        public static bool BeginNormalQuit()
        {
            if (WindowCloseRecorded || QuittingNormally)
                return false;
            QuittingNormally = true;

            SaveManager.Data.exitState = ExitState.Normal;
            SaveManager.Save();
            return true;
        }
    }
}
