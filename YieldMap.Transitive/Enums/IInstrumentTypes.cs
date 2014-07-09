using YieldMap.Database;

namespace YieldMap.Transitive.Enums {
    public interface IInstrumentTypes {
        InstrumentType Bond { get; }
        InstrumentType Frn { get; }
        InstrumentType Swap { get; }
        InstrumentType Ndf { get; }
        InstrumentType Cds { get; }
    }
}