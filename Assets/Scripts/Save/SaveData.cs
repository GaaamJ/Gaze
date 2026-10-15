using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Save
{
    // JsonUtility는 enum을 숫자로 저장하므로 기존 값은 바꾸지 않고 새 값은 뒤에 추가한다.
    public enum ExitState
    {
        None = 0,
        Normal = 1,
        DeathPendingFirst = 2,
        DeathPendingRetrigger = 3
    }

    public enum CorpseKind
    {
        World = 0,
        File = 1
    }

    [Serializable]
    public class DeathRecord
    {
        public string deathId;
        public int count;
    }

    [Serializable]
    public class CorpseRecord
    {
        public string id;
        public Vector3 position;
        public string areaId;
        public CorpseKind kind;
        public string fileName;
    }

    [Serializable]
    public class SaveData
    {
        public List<string> solvedPuzzleIds = new List<string>();
        public List<DeathRecord> deathRecords = new List<DeathRecord>();
        public List<CorpseRecord> corpses = new List<CorpseRecord>();

        // 비어 있으면 시작 지점
        public string respawnAreaId = "";

        // false면 씬의 라리스 기본 위치에서 시작
        public bool hasRespawnPosition;
        public Vector2 respawnPosition;

        public List<string> investigatedNodeIds = new List<string>();

        // 사망 노드 이름이 사망의 키다. 같은 노드로 여러 번 죽어도 한 번만 남는다.
        public List<string> diedNodeIds = new List<string>();

        public string elevatorAreaId = "";

        public string elevatorFileId = "";
        public List<string> elevatorFileIdHistory = new List<string>();

        public ExitState exitState = ExitState.None;
    }
}
