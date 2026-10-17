using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ProjectT.Core.Elevator;
using ProjectT.Core.GameFolder;
using ProjectT.Data;
using ProjectT.Save;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectT.DevTools
{
    // 사람 시험용 임시 대역. 게임 코드는 이 컴포넌트를 참조하지 않으며, 사망은 호출하지 않는다.
    public class ElevatorJudgeDebugger : MonoBehaviour
    {
        const string Tag = "[ElevatorJudge]";
        const float Interval = 0.5f;

        [SerializeField] AreaData[] areas;

        List<string> floorNames;
        float elapsed;
        bool warnedEmptyId;
        bool hasLast;
        ElevatorVerdict lastVerdict;
        string lastFloor;
        string lastReason;
        string loggedFloor;

        void Start()
        {
            floorNames = AreaFloorFolders.Names(areas);
            RunJudge();
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < Interval) return;
            elapsed = 0f;
            RunJudge();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && floorNames != null)
                RunJudge();
        }

        void RunJudge()
        {
            var data = SaveManager.Data;
            if (string.IsNullOrEmpty(data.elevatorFileId))
            {
                if (!warnedEmptyId)
                {
                    Debug.LogWarning($"{Tag} 현재 값이 비어 있어 판정을 쉰다. 컨텍스트 메뉴 '준비'를 실행한다.", this);
                    warnedEmptyId = true;
                }
                return;
            }
            warnedEmptyId = false;

            var result = ElevatorFileJudge.Judge(SaveManager.Paths, floorNames, data.elevatorFileId, data.elevatorFileIdHistory);
            if (hasLast && result.Verdict == lastVerdict && result.Floor == lastFloor && result.Reason == lastReason)
                return;

            hasLast = true;
            lastVerdict = result.Verdict;
            lastFloor = result.Floor;
            lastReason = result.Reason;

            if (!result.IsViolation)
            {
                Debug.Log($"{Tag} 정상 | Floor={result.Floor} | {result.Reason}", this);
                if (loggedFloor != null && !string.Equals(loggedFloor, result.Floor, StringComparison.OrdinalIgnoreCase))
                    Debug.Log($"{Tag} 이동: {loggedFloor} → {result.Floor}", this);
                loggedFloor = result.Floor;
                return;
            }

            Debug.Log($"{Tag} 비정상 조작 감지 ← {VerdictName(result.Verdict)} | {result.Reason}", this);
            if (result.Verdict == ElevatorVerdict.Fake)
                LogFakeContents(data.elevatorFileId, data.elevatorFileIdHistory);
        }

        void LogFakeContents(string currentId, List<string> history)
        {
            var paths = SaveManager.Paths;
            foreach (var floor in floorNames)
            {
                if (!GameFolderPaths.TryNormalizeFolderName(floor, out var name)) continue;
                string dir = paths.FloorFolder(name);
                if (!Directory.Exists(dir)) continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        if (!string.Equals(Path.GetExtension(file), ".ev", StringComparison.OrdinalIgnoreCase)) continue;
                        if (ElevatorFile.TryReadId(file, out var id) && history.Contains(id)) continue;

                        bool contains = File.ReadAllText(file).IndexOf(currentId, StringComparison.OrdinalIgnoreCase) >= 0;
                        Debug.Log($"{Tag}   {name}\\{Path.GetFileName(file)} 본문에 현재 GUID 포함: {(contains ? "예" : "아니오")}", this);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"{Tag}   {name} 가짜 파일 본문 확인 실패: {e.Message}", this);
                }
            }
        }

        static string VerdictName(ElevatorVerdict verdict)
        {
            switch (verdict)
            {
                case ElevatorVerdict.Fake: return "가짜";
                case ElevatorVerdict.Copy: return "복사";
                case ElevatorVerdict.Deleted: return "삭제";
                case ElevatorVerdict.Renamed: return "이름 변경";
                default: return "정상";
            }
        }

        // 실제 save.json과 Salbyeol\을 바꾼다. 정리는 Tools > 데이터 초기화.
        [ContextMenu("준비")]
        void Prepare()
        {
            // 에디트 모드에서는 SaveManager.Data가 로드되지 않은 기본값일 수 있어, 저장하면 실제 세이브를 덮어쓴다.
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{Tag} '준비'는 플레이 중에만 실행한다.", this);
                return;
            }
            if (areas == null || areas.Length == 0 || areas[0] == null)
            {
                Debug.LogWarning($"{Tag} areas가 비어 있어 준비할 수 없다.", this);
                return;
            }

            if (!ElevatorFileIds.Issue(out var id))
                Debug.LogWarning($"{Tag} 현재 값 세이브 실패. 메모리 값으로 계속한다.", this);

            var paths = SaveManager.Paths;
            var ensure = AreaFloorFolders.EnsureStructure(paths, areas);
            if (!ensure.Succeeded)
                Debug.LogWarning($"{Tag} 층 폴더 보장 실패 {ensure.Failed.Count}건, 잘못된 이름 {ensure.InvalidFloorNames.Count}건", this);

            string file = AreaFloorFolders.ElevatorFile(paths, areas[0]);
            if (!ElevatorFileIds.WriteCurrent(file))
            {
                Debug.LogWarning($"{Tag} 엘리베이터 파일 생성 실패: {file}", this);
                return;
            }

            floorNames = AreaFloorFolders.Names(areas);
            loggedFloor = areas[0].FloorFolderName;
            hasLast = false;
            Debug.Log($"{Tag} 준비 완료: {file} (현재 값 {id})", this);
            RunJudge();
        }

        [ContextMenu("판정 1회 시간 측정")]
        void MeasureJudge()
        {
            var data = SaveManager.Data;
            var names = AreaFloorFolders.Names(areas);
            var watch = Stopwatch.StartNew();
            ElevatorFileJudge.Judge(SaveManager.Paths, names, data.elevatorFileId, data.elevatorFileIdHistory);
            watch.Stop();
            Debug.Log($"{Tag} 판정 1회: {watch.Elapsed.TotalMilliseconds:0.000} ms", this);
        }
    }
}
