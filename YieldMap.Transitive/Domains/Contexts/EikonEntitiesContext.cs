using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class EikonEntitiesContext : BaseContext<EikonEntitiesContext> {
        // todo create reference entities for externals
        public DbSet<Chain> Chains;
        public DbSet<Ric> Rics;
        public DbSet<Feed> Feeds;
        public DbSet<RicToChain> RicToChains;
        public DbSet<Index> Indices;
        public DbSet<Isin> Isins;
    }
}
