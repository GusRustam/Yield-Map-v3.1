using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class InstrumentDescriptionContext : BaseContext<InstrumentDescriptionContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.InstrumentDescriptionContext");
        public InstrumentDescriptionContext() {
            Logger.Debug("InstrumentDescriptionContext()");
        }  
        
        
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<Description> Descriptions { get; set; }
        public DbSet<InstrumentDescriptionView> InstrumentDescriptionViews { get; set; }
        public DbSet<BondDescriptionView> BondDescriptionViews { get; set; }
    }
}
