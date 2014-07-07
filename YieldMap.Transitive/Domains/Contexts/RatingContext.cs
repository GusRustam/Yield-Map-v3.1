using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
