using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class InstrumentDescriptionContext : BaseContext<InstrumentDescriptionContext> {
        public DbSet<Instrument> Instruments;
        public DbSet<Description> Descriptions;
        public DbSet<InstrumentDescriptionView> InstrumentDescriptionViews;
    }
}
