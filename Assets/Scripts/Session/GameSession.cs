using System.Collections.Generic;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Session
{
    // 게임을 켤 때마다 세이브를 한 번 불러와 세션 메모리(씬을 다시 불러와도 유지)를 다시 채운다.
    public static class GameSession
    {
        // 조사를 마친 DialogueInteractable의 dialogueNodeId. 같은 dialogueNodeId를 쓰는 오브젝트는 기록을 공유한다.
        public static HashSet<string> Investigated { get; private set; }

        // 정적 필드 초기화 대신 여기서 만드는 이유: 도메인 리로드를 끈 플레이 모드에서도 실행마다 세이브 기준으로 다시 채우기 위함.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Begin()
        {
            // 첫 씬의 컴포넌트가 SaveManager.Data를 읽기 전에 로드해 두려고 BeforeSceneLoad 시점에 부른다.
            SaveManager.Load();
            Investigated = new HashSet<string>(SaveManager.Data.investigatedNodeIds);
        }
    }
}
