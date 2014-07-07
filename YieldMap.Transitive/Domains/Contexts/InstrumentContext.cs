using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class InstrumentContext : BaseContext<InstrumentContext> {
        public DbSet<Instrument> Instruments { get; set; }
        
    }
}
