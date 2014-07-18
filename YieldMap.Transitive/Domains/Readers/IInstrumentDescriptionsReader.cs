using System.Linq;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Readers {
    public interface IInstrumentDescriptionsReader  {
        IQueryable<InstrumentDescriptionView> InstrumentDescriptionViews { get; }
        IQueryable<Instrument> Instruments { get; }
        IQueryable<Description> Descriptions { get; }
    }
}