namespace ProjectT.Core.Elevator
{
    public enum ElevatorVerdict
    {
        Normal,
        Fake,
        Copy,
        Deleted,
        Renamed
    }

    public sealed class ElevatorJudgement
    {
        public ElevatorVerdict Verdict { get; }
        public string Floor { get; }
        public string Reason { get; }
        public bool IsViolation => Verdict != ElevatorVerdict.Normal;

        public ElevatorJudgement(ElevatorVerdict verdict, string floor, string reason)
        {
            Verdict = verdict;
            Floor = verdict == ElevatorVerdict.Normal ? floor : null;
            Reason = reason ?? "";
        }
    }
}
