using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class ChainRicContext : BaseContext<ChainRicContext> {
        public DbSet<RicToChain> RicToChains { get; set; }
        public DbSet<OrdinaryBond> OrdinaryBonds { get; set; }
        public DbSet<Chain> Chains { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<Ric> Rics { get; set; }
    }
}
