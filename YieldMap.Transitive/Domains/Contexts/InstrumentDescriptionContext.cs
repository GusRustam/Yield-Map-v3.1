using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class InstrumentDescriptionContext : BaseContext<InstrumentDescriptionContext> {
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<Description> Descriptions { get; set; }
        public DbSet<InstrumentDescriptionView> InstrumentDescriptionViews { get; set; }
    }
}
