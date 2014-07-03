using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.ReadOnly {
    public interface IInstrumentDescriptionsReader {
        IQueryable<InstrumentDescriptionView> InstrumentDescriptionViews { get; }
        IQueryable<Instrument> Instruments { get; }
        IQueryable<Description> Descriptions { get; }
        Dictionary<string, object> PackInstrumentDescription(InstrumentDescriptionView i);
    }
}