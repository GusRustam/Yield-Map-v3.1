using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class ChainRicContext : BaseContext<ChainRicContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.ChainRicContext");
        public ChainRicContext() {
            Logger.Debug("ChainRicContext()");
        }

        public DbSet<RicToChain> RicToChains { get; set; }
        public DbSet<OrdinaryBond> OrdinaryBonds { get; set; }
        public DbSet<Chain> Chains { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<Ric> Rics { get; set; }
    }
}
