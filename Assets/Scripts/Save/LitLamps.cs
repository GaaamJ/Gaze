namespace Gaze.Save
{
    // 켠 등불의 기록(SaveData.litLampIds). LampResponse가 남기고 RoomLight가 읽음.
    public static class LitLamps
    {
        // 등불을 기록하고 저장. 저장 성공 여부를 돌려줌.
        public static bool Add(string lampId) =>
            SaveManager.AddUnique(SaveManager.Data.litLampIds, lampId);

        public static bool Contains(string lampId) =>
            !string.IsNullOrEmpty(lampId) && SaveManager.Data.litLampIds.Contains(lampId);
    }
}
