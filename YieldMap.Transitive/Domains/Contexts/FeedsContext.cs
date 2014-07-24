using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class FeedsContext : BaseContext<FeedsContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.FeedsContext");
        public FeedsContext() {
            Logger.Debug("FeedsContext()");
        }  
        
        public DbSet<Feed> Feeds { get; set; }
        //public DbSet<Chain> Chains { get; set; }
        //public DbSet<Ric> Rics { get; set; }
        //public DbSet<Feed> Feeds { get; set; }
        //public DbSet<RicToChain> RicToChains { get; set; }
        //public DbSet<Index> Indices { get; set; }
        //public DbSet<Isin> Isins { get; set; }
    }
}
