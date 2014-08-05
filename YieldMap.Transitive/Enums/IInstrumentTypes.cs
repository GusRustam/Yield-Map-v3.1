using YieldMap.Database;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public interface IInstrumentTypes {
        NInstrumentType Bond { get; }
        NInstrumentType Frn { get; }
        NInstrumentType Swap { get; }
        NInstrumentType Ndf { get; }
        NInstrumentType Cds { get; }
    }
}