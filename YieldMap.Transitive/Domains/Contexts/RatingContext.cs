using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class RatingContext : BaseContext<RatingContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.RatingContext");
        public RatingContext() {
            Logger.Debug("RatingContext()");
        }  
        
        public DbSet<RatingToInstrument> RatingToInstruments { get; set; }
        public DbSet<RatingToLegalEntity> RatingToLegalEntities { get; set; }
        
    }
}
