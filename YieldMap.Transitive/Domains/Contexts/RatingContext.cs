using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class RatingContext : BaseContext<RatingContext> {
        public DbSet<InstrumentRicView> InstrumentRicViews { get; set; }
        public DbSet<RatingToInstrument> RatingToInstruments { get; set; }
        public DbSet<InstrumentIBView> InstrumentIBViews { get; set; }
        public DbSet<RatingsView> RatingsViews { get; set; }
        public DbSet<RatingToLegalEntity> RatingToLegalEntities { get; set; }
        
    }
}
