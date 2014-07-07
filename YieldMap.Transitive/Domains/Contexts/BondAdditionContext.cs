using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class BondAdditionContext : BaseContext<BondAdditionContext> {
        public DbSet<Description> Descriptions { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<LegalEntity> LegalEntities { get; set; }
        public DbSet<Ticker> Tickers { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<SubIndustry> SubIndustries { get; set; }
        public DbSet<Specimen> Specimens { get; set; }
        public DbSet<Seniority> Seniorities { get; set; }
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<InstrumentType> InstrumentTypes { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Index> Indices { get; set; } // todo ??
        public DbSet<Ric> Rics { get; set; } // todo ??
    }
}
