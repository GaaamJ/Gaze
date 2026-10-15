namespace ProjectT.Save
{
    // M3 예약: 아직 런타임 호출처가 없지만 M3 리스폰 지역 처리에 쓸 것이니 지우지 않는다.
    public static class RespawnAreaIds
    {
        public static string Current => SaveManager.Data.respawnAreaId;

        // 저장에 실패해도 메모리 값은 바뀐 상태로 남아 다음 저장 때 함께 기록된다.
        public static bool Set(string areaId)
        {
            SaveManager.Data.respawnAreaId = areaId ?? "";
            return SaveManager.Save();
        }
    }
}
