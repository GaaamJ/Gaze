using System.Collections.Generic;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Session
{
    // 게임을 켤 때마다 세션 메모리(씬을 다시 불러와도 유지)를 비우고 세이브를 한 번 불러온다.
    public static class GameSession
    {
        // 조사를 마친 Interactable의 nodeId. 같은 nodeId를 쓰는 오브젝트는 기록을 공유한다.
        public static HashSet<string> Investigated { get; private set; }

        // 정적 필드 초기화 대신 여기서 만드는 이유: 도메인 리로드를 끈 플레이 모드에서도 실행마다 비우기 위함.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Begin()
        {
            Investigated = new HashSet<string>();
            // 첫 씬의 컴포넌트가 SaveManager.Data를 읽기 전에 로드해 두려고 BeforeSceneLoad 시점에 부른다.
            SaveManager.Load();
        }
    }
}
