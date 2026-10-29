using ProjectT.Core.GameFolder;
using UnityEngine;

namespace ProjectT.Save
{
    // 게임이 어떻게 종료됐는지 세이브에 남김. 창 닫기 사망과 정상 종료를 다룸.
    // 창 닫기(빌드 종료 훅·WM_ENDSESSION·에디터 시뮬레이션)와 정상 종료가 같은 플래그를 보고 서로를 막음.
    // 호출자: QuitHooks·WindowsSessionEndHook(RecordWindowClose), NormalQuit(BeginNormalQuit), GameSession(TakeLastExitState).
    public static class ExitRecorder
    {
        // 창 닫기 사망에 쓰는 사망 노드 이름. 재시작 때 CoreEntry가 이 노드를 재생함.
        public const string WindowCloseDeathNodeId = "Death_CloseWindow";

        // 이번 실행에서 창 닫기 사망을 이미 기록했는지.
        public static bool WindowCloseRecorded { get; private set; }
        // 이번 실행이 정상 종료로 들어갔는지.
        public static bool QuittingNormally { get; private set; }

        // 도메인 리로드를 끈 플레이 모드에서도 실행마다 플래그를 비움.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetState()
        {
            WindowCloseRecorded = false;
            QuittingNormally = false;
        }

        // 창 닫기 사망 기록. 종료 상태 DeathPending·사망 노드 저장 → 시체 파일 생성.
        // 이미 기록했거나 정상 종료 중이면 아무것도 하지 않음. 저장에 실패하면 시체 파일을 만들지 않음.
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

        // 정상 종료 기록. 종료 상태 Normal 저장. 종료를 진행해도 되면 참, 창 닫기가 먼저 기록됐거나 이미 정상 종료 중이면 거짓.
        // 저장에 실패해도 종료는 진행함. 디스크의 종료 상태는 시작 때 None으로 비워 두었으므로 다음 실행은 튕김으로 봄.
        public static bool BeginNormalQuit()
        {
            if (WindowCloseRecorded || QuittingNormally)
                return false;
            QuittingNormally = true;

            SaveManager.Data.exitState = ExitState.Normal;
            SaveManager.Save();
            return true;
        }

        // 지난 실행의 종료 상태를 읽어 돌려줌. 실행 시작 때 GameSession.Begin이 부름.
        // 읽은 값은 바로 None으로 저장해 다음 실행에서 다시 처리하지 않음.
        public static ExitState TakeLastExitState()
        {
            SaveData data = SaveManager.Data;
            ExitState state = Normalize(data.exitState);
            if (data.exitState != ExitState.None)
            {
                data.exitState = ExitState.None;
                SaveManager.Save();
            }
            return state;
        }

        static ExitState Normalize(ExitState state) =>
            state == ExitState.Normal || state == ExitState.DeathPending ? state : ExitState.None;
    }
}
