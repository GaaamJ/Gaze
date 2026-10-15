using ProjectT.Core.GameFolder;

namespace ProjectT.Save
{
    // M3 예약: 아직 런타임 호출처가 없지만 M3 엘리베이터 파일 처리에 쓸 것이니 지우지 않는다.
    public static class ElevatorFileIds
    {
        public static string Current => SaveManager.Data.elevatorFileId;

        public static bool Issue(out string id)
        {
            id = ElevatorFile.NewId();
            var data = SaveManager.Data;
            data.elevatorFileId = id;
            data.elevatorFileIdHistory.Add(id);
            return SaveManager.Save();
        }

        public static bool WriteCurrent(string filePath) =>
            ElevatorFile.Write(filePath, Current);
    }
}
