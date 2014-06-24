namespace YieldMap.Database.StoredProcedures.Enums {
    public interface IInstrumentTypes {
        InstrumentType Bond { get; }
        InstrumentType Frn { get; }
        InstrumentType Swap { get; }
        InstrumentType Ndf { get; }
        InstrumentType Cds { get; }
    }
}