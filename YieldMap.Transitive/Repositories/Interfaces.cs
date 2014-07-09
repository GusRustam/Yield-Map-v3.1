using YieldMap.Database;
using YieldMap.Transitive.Domains;

namespace YieldMap.Transitive.Repositories {
    public interface IFeedRepository : IRepository<Feed> {}
    public interface IDescriptionRepository : IRepository<Description> {}
    public interface ICountryRepository : IRepository<Country> {}
    public interface ILegalEntityRepository : IRepository<LegalEntity> {}
    public interface ITickerRepository : IRepository<Ticker> {}
    public interface IIndustryRepository : IRepository<Industry> {}
    public interface ISubIndustryRepository : IRepository<SubIndustry> {}
    public interface ISpecimenRepository : IRepository<Specimen> {}
    public interface ISeniorityRepository : IRepository<Seniority> {}
    public interface IInstrumentTypeRepository : IRepository<InstrumentType> {}
    public interface IChainRepository : IRepository<Chain> {}
    public interface IRicToChainRepository : IRepository<RicToChain> {}
    public interface IIndexRepository : IRepository<Index> {}
}