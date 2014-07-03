using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Repositories {
    public interface IFeedRepository : IRepository<Feed> {}
    public interface IInstrumentRepository : IRepository<Instrument> {}
    public interface IDescriptionRepository : IRepository<IDescriptionRepository> {}
    public interface ICountryRepository : IRepository<ICountryRepository> {}
    public interface ILegalEntityRepository : IRepository<ILegalEntityRepository> {}
    public interface ITickerRepository : IRepository<ITickerRepository> {}
    public interface IIndustryRepository : IRepository<IIndustryRepository> {}
    public interface ISubIndustryRepository : IRepository<ISubIndustryRepository> {}
    public interface ISpecimenRepository : IRepository<ISpecimenRepository> {}
    public interface ISeniorityRepository : IRepository<ISeniorityRepository> {}
    public interface IInstrumentTypeRepository : IRepository<IInstrumentTypeRepository> {}
    public interface IChainRepository : IRepository<IChainRepository> {}
    public interface IRicRepository : IRepository<IRicRepository> {}
    public interface IRicToChainRepository : IRepository<IRicToChainRepository> {}
    public interface IIndexRepository : IRepository<IIndexRepository> {}
}