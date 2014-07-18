using System.Collections.Generic;
using YieldMap.Database;

namespace YieldMap.Transitive.Tools {
    public interface IInstrumentPacker {
        Dictionary<string, object> PackInstrumentDescription(InstrumentDescriptionView i);
    }
}