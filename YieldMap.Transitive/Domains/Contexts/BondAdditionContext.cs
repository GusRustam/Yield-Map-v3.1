using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class BondAdditionContext : BaseContext<BondAdditionContext> {
        public DbSet<Description> Descriptions;
        public DbSet<Country> Countries;
        public DbSet<LegalEntity> LegalEntities;
        public DbSet<Ticker> Tickers;
        public DbSet<Industry> Industries;
        public DbSet<SubIndustry> SubIndustries;
        public DbSet<Specimen> Specimens;
        public DbSet<Seniority> Seniorities;
        public DbSet<Instrument> Instruments;
        public DbSet<InstrumentType> InstrumentTypes;
        public DbSet<Currency> Currencies;
        public DbSet<Index> Indices; // todo ??
        public DbSet<Ric> Rics; // todo ??
    }
}
