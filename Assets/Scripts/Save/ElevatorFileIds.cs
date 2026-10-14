using ProjectT.Core.GameFolder;

namespace ProjectT.Save
{
    public static class ElevatorFileIds
    {
        public static string Current => SaveManager.Data.elevatorFileId;

        // 저장에 실패해도 메모리의 현재 값·이력은 바뀐 상태로 남는다. 반환값으로 저장 성공 여부를 알린다.
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
