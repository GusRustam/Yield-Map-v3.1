using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class RatingContext : BaseContext<RatingContext> {
        public DbSet<InstrumentRicView> InstrumentRicViews;
        public DbSet<RatingToInstrument> RatingToInstruments;
        public DbSet<InstrumentIBView> InstrumentIBViews;
        public DbSet<RatingsView> RatingsViews;
        public DbSet<RatingToLegalEntity> RatingToLegalEntities;
        
    }
}
