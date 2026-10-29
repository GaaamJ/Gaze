using System.Collections.Generic;
using UnityEngine;
using Gaze.Save;

namespace Gaze.Session
{
    // 게임을 켤 때마다 세이브를 한 번 불러와 세션 메모리(씬을 다시 불러와도 유지)를 다시 채움.
    // 세션 메모리: 조사 기록(Investigated)과 창 닫기 사망 대기 여부.
    public static class GameSession
    {
        // 조사를 마친 DialogueInteractable의 dialogueNodeId와 재생을 마친 DeathRefusalZone의 refusalNodeId.
        // 같은 노드 이름을 쓰는 오브젝트는 기록을 공유함.
        // PlayerInteractor가 상호작용 때 넘기고 DeathRefusalZone이 읽음.
        public static HashSet<string> Investigated { get; private set; }

        static bool pendingWindowCloseDeath;

        // 실행 시작 때 한 번 호출됨. 세이브 로드 → 조사 기록 채움 → 지난 실행의 종료 상태를 읽어 창 닫기 사망 대기 여부를 정함.
        // 정적 필드 초기화 대신 여기서 만드는 이유: 도메인 리로드를 끈 플레이 모드에서도 실행마다 세이브 기준으로 다시 채우기 위함.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Begin()
        {
            // 첫 씬의 컴포넌트가 SaveManager.Data를 읽기 전에 로드해 두려고 BeforeSceneLoad 시점에 부름.
            Reload();
            pendingWindowCloseDeath = ExitRecorder.TakeLastExitState() == ExitState.DeathPending;
        }

        // 세이브를 다시 읽어 조사 기록을 채우고 사망 대기를 비움.
        // 종료 상태는 건드리지 않음. 테스트가 실제 세이브로 되돌릴 때 사망 대기를 소비하지 않게 하기 위함.
        internal static void Reload()
        {
            SaveManager.Load();
            Investigated = new HashSet<string>(SaveManager.Data.investigatedNodeIds);
            pendingWindowCloseDeath = false;
        }

        // 창 닫기 사망 대기를 한 번만 참으로 돌려주고 비움. CoreEntry.EnterScene이 부름.
        public static bool ConsumePendingDeath()
        {
            if (!pendingWindowCloseDeath)
                return false;
            pendingWindowCloseDeath = false;
            return true;
        }
    }
}
