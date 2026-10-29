namespace ProjectT.Session
{
    // InputLock이 입력을 잠근 사유. 사유별로 건 횟수를 따로 셈.
    // Dialogue는 DialogueService, RoomMove는 RoomSceneLoader, Death는 DeathSequence·CoreEntry가 검.
    public enum InputLockReason
    {
        Dialogue,
        Death,
        RoomMove,
    }
}
