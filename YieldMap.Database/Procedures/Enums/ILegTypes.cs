namespace YieldMap.Database.Procedures.Enums {
    public interface ILegTypes {
        LegType Paid { get; }
        LegType Received { get; }
        LegType Both { get; }
    }
}