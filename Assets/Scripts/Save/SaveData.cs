using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Save
{
    // JsonUtility는 enum을 숫자로 저장하므로 기존 값은 바꾸지 않고 새 값은 뒤에 추가한다.
    // 3은 폐기한 재발동 대기(옛 DeathPendingRetrigger) 값이라 다시 쓰지 않는다. 읽을 때 None으로 본다.
    public enum ExitState
    {
        None = 0,
        Normal = 1,
        DeathPending = 2
    }

    // 필드 이름이 JSON 키라서 이름을 바꾸면 옛 세이브의 값이 조용히 기본값이 된다.
    [Serializable]
    public class SaveData
    {
        public List<string> solvedPuzzleIds = new List<string>();

        // false면 씬의 라리스 기본 위치에서 시작
        public bool hasRespawnPosition;
        public Vector2 respawnPosition;

        // 시작할 때 Core가 불러올 Room 씬 이름. 비어 있으면 기본 Room
        public string roomName = "";

        public List<string> investigatedNodeIds = new List<string>();

        // 사망 노드 이름이 사망의 키다. 같은 노드로 여러 번 죽어도 한 번만 남는다.
        public List<string> diedNodeIds = new List<string>();

        public ExitState exitState = ExitState.None;
    }
}
