using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gaze.Save
{
    // 지난 실행이 어떻게 끝났는지. 다음 실행 시작 때 ExitRecorder.TakeLastExitState가 읽음.
    // None은 기록 없음(튕김 포함), Normal은 정상 종료, DeathPending은 창 닫기 사망 대기.
    // JsonUtility는 enum을 숫자로 저장하므로 기존 값은 바꾸지 않고 새 값은 뒤에 추가함.
    // 3은 폐기한 값이라 다시 쓰지 않음. 읽을 때 None으로 봄.
    public enum ExitState
    {
        None = 0,
        Normal = 1,
        DeathPending = 2
    }

    // save.json에 저장되는 세이브 내용. SaveManager가 읽고 씀.
    // 필드 이름이 JSON 키라서 이름을 바꾸면 옛 세이브의 값이 조용히 기본값이 됨.
    [Serializable]
    public class SaveData
    {
        public List<string> solvedPuzzleIds = new List<string>();

        // false면 씬의 라리스 기본 위치에서 시작함.
        public bool hasRespawnPosition;
        public Vector2 respawnPosition;

        // 시작할 때 Core가 불러올 Room 씬 이름. 비어 있으면 기본 Room.
        public string roomName = "";

        public List<string> investigatedNodeIds = new List<string>();

        // 사망 노드 이름이 사망의 키. 같은 노드로 여러 번 죽어도 한 번만 남음.
        public List<string> diedNodeIds = new List<string>();

        public ExitState exitState = ExitState.None;
    }
}
