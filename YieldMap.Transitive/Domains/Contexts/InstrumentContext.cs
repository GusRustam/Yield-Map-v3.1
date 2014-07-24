using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class InstrumentContext : BaseContext<InstrumentContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.InstrumentContext");
        public InstrumentContext() {
            Logger.Debug("InstrumentContext()");
        }  
        
        public DbSet<Instrument> Instruments { get; set; }
        
    }
}
