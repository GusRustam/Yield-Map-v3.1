using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Readers {
    public class InstrumentDescriptionsReader : ReadOnlyRepository<InstrumentDescriptionContext>,
        IInstrumentDescriptionsReader {
        public InstrumentDescriptionsReader() {
        }

        public InstrumentDescriptionsReader(InstrumentDescriptionContext context) : base(context) {
        }

        public IQueryable<InstrumentDescriptionView> InstrumentDescriptionViews {
            get { return Context.InstrumentDescriptionViews.AsNoTracking(); }
        }

        public IQueryable<Instrument> Instruments {
            get { return Context.Instruments.AsNoTracking(); }
        }

        public IQueryable<Description> Descriptions {
            get { return Context.Descriptions.AsNoTracking(); }
        }
    }
}