using System;
using System.Collections.Generic;
using System.IO;
using ProjectT.Core.GameFolder;

namespace ProjectT.Core.Elevator
{
    public static class ElevatorFileJudge
    {
        const string Extension = ".ev";

        public static ElevatorJudgement Judge(
            GameFolderPaths paths,
            IReadOnlyList<string> floorFolderNames,
            string currentId,
            IReadOnlyCollection<string> idHistory)
        {
            ElevatorFile.TryNormalizeId(currentId, out var current);
            var history = NormalizeHistory(idHistory);

            var currentFiles = new List<string>();
            var oldFiles = new List<string>();
            var fakeFiles = new List<string>();
            string currentFloor = null;
            string currentFileName = null;

            foreach (var floor in DistinctFloors(floorFolderNames))
            {
                foreach (var file in CollectEvFiles(paths, floor))
                {
                    string fileName = Path.GetFileName(file);
                    string label = floor + "\\" + fileName;

                    if (!ElevatorFile.TryReadId(file, out var id))
                    {
                        fakeFiles.Add(label + " 가짜(읽기 실패·손상)");
                    }
                    else if (current != null && id == current)
                    {
                        currentFiles.Add(label);
                        currentFloor = floor;
                        currentFileName = fileName;
                    }
                    else if (history.Contains(id))
                    {
                        oldFiles.Add(label);
                    }
                    else
                    {
                        fakeFiles.Add(label + " 가짜(이력에 없는 값)");
                    }
                }
            }

            string oldNote = oldFiles.Count > 0 ? " | 옛 값 제외: " + string.Join(", ", oldFiles) : "";

            if (fakeFiles.Count > 0)
                return new ElevatorJudgement(ElevatorVerdict.Fake, null, string.Join(", ", fakeFiles) + oldNote);

            if (currentFiles.Count >= 2)
                return new ElevatorJudgement(ElevatorVerdict.Copy, null, string.Join(", ", currentFiles) + " 모두 현재 값" + oldNote);

            if (currentFiles.Count == 0)
                return new ElevatorJudgement(ElevatorVerdict.Deleted, null, "층 폴더에 현재 값 파일 없음" + oldNote);

            if (!string.Equals(currentFileName, GameFolderPaths.ElevatorFileName, StringComparison.Ordinal))
                return new ElevatorJudgement(ElevatorVerdict.Renamed, null,
                    currentFiles[0] + " (정상 이름: " + GameFolderPaths.ElevatorFileName + ")" + oldNote);

            return new ElevatorJudgement(ElevatorVerdict.Normal, currentFloor, currentFiles[0] + "(현재 값)" + oldNote);
        }

        static HashSet<string> NormalizeHistory(IReadOnlyCollection<string> idHistory)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (idHistory == null) return set;

            foreach (var raw in idHistory)
            {
                if (ElevatorFile.TryNormalizeId(raw, out var id))
                    set.Add(id);
            }
            return set;
        }

        static List<string> DistinctFloors(IReadOnlyList<string> floorFolderNames)
        {
            var floors = new List<string>();
            if (floorFolderNames == null) return floors;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in floorFolderNames)
            {
                if (GameFolderPaths.TryNormalizeFolderName(raw, out var name) && seen.Add(name))
                    floors.Add(name);
            }
            return floors;
        }

        static List<string> CollectEvFiles(GameFolderPaths paths, string floor)
        {
            var files = new List<string>();
            if (paths == null) return files;

            try
            {
                string dir = paths.FloorFolder(floor);
                if (!Directory.Exists(dir)) return files;

                // "*.ev" 패턴은 8.3 짧은 이름 때문에 ".evx" 같은 확장자까지 잡을 수 있어 직접 거른다.
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (string.Equals(Path.GetExtension(file), Extension, StringComparison.OrdinalIgnoreCase))
                        files.Add(file);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            {
                // 폴더를 읽지 못하면 빈 층으로 본다. 판정 불가 분리는 #65.
            }

            files.Sort(StringComparer.Ordinal);
            return files;
        }
    }
}
