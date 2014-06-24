namespace YieldMap.Database.StoredProcedures.Enums {
    public interface ILegTypes {
        LegType Paid { get; }
        LegType Received { get; }
        LegType Both { get; }
    }
}