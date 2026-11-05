namespace Gaze.Save
{
    // 플레이어 이름의 기록(SaveData.playerName). 정하는 즉시 저장함.
    // PlayerNameCommand가 남기고 PlayerNameFunctions가 읽음.
    public static class PlayerName
    {
        public static string Saved => SaveManager.Data.playerName;

        public static bool HasName => !string.IsNullOrWhiteSpace(Saved);

        // 앞뒤 공백을 지우고 기록·저장. 비어 있으면 기록하지 않고 false.
        public static bool Set(string name)
        {
            string trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return false;

            SaveManager.Data.playerName = trimmed;
            return SaveManager.Save();
        }
    }
}
