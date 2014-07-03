using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class ChainRicContext : BaseContext<ChainRicContext> {
        public DbSet<RicToChain> RicToChains;
        public DbSet<OrdinaryBond> OrdinaryBonds;
        public DbSet<Chain> Chains;
        public DbSet<Feed> Feeds;
        public DbSet<Ric> Rics;
    }
}
