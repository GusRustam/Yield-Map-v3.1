using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Queries;

namespace YieldMap.Transitive.Domains.Repositories {
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

    public interface IInstrumentRepository : IRepository<Instrument> { }
    public class InstrumentRepository : IInstrumentRepository {
        private readonly InstrumentContext _context;

        public InstrumentRepository(IUnitOfWork<InstrumentContext> uow) {
            _context = uow.Context;
        }

        public InstrumentRepository() {
            _context = new InstrumentContext();
        }

        public IQueryable<Instrument> FindAll() {
            return _context.Instruments;
        }

        public IQueryable<Instrument> FindAllIncluding(params Expression<Func<Instrument, object>>[] inc) {
            return inc.Aggregate<Expression<Func<Instrument, object>>, IQueryable<Instrument>>(
                _context.Instruments,
                (current, expression) => current.Include(expression));
        }

        public IQueryable<Instrument> FindBy(Func<Instrument, bool> predicate) {
            return _context.Instruments.Where(predicate).AsQueryable();
        }

        public Instrument FindById(long id) {
            return _context.Instruments.Find(id);
        }

        public void Insert(Instrument item) {
            _context.Instruments.Add(item);
            if (item.State != State.Added)
                _context.ApplyStateChanges();
        }

        public void Add(Instrument item) {
            _context.Entry(item).State = item.id == default(long) ?
                EntityState.Added :
                EntityState.Modified;
        }

        public void Remove(Instrument item) {
            _context.Instruments.Remove(item);
        }

        public void Dispose() {
            _context.Dispose();
        }
    }

    public class ChainRepository : IChainRepository {
        private readonly ChainRicContext _context;

        public ChainRepository(IUnitOfWork<ChainRicContext> uow) {
            _context = uow.Context;
        }

        public ChainRepository() {
            _context = new ChainRicContext();
        }
        
        public IQueryable<Chain> FindAll() {
            return _context.Chains;
        }

        public IQueryable<Chain> FindAllIncluding(params Expression<Func<Chain, object>>[] inc) {
            return inc.Aggregate<Expression<Func<Chain, object>>, IQueryable<Chain>>(
                _context.Chains, 
                (current, expression) => current.Include(expression));
        }

        public IQueryable<Chain> FindBy(Func<Chain, bool> predicate) {
            return _context.Chains.Where(predicate).AsQueryable();
        }

        public Chain FindById(long id) {
            return _context.Chains.Find(id);
        }

        public void Insert(Chain item) {
            _context.Chains.Add(item);
            if (item.State != State.Added) 
                _context.ApplyStateChanges();
        }

        public void Add(Chain item) {
            _context.Entry(item).State = item.id == default(long) ? 
                EntityState.Added : 
                EntityState.Modified;
        }

        public void Remove(Chain item) {
            _context.Chains.Remove(item);
        }

        public void Dispose() {
            _context.Dispose();
        }
    }
    public interface IChainRicUnitOfWork : IUnitOfWork<ChainRicContext> {
    }
    public class ChainRicUnitOfWork : IChainRicUnitOfWork {
        public ChainRicUnitOfWork() {
            Context = new ChainRicContext();
        }

        public ChainRicUnitOfWork(ChainRicContext context) {
            Context = context;
        }

        public void Dispose() {
            Context.Dispose();
        }

        public int Save() {
            return Context.SaveChanges();
        }

        public ChainRicContext Context { get; private set; }
    }

    public interface IRicRepository : IRepository<Ric> {}
    public class RicRepository : IRicRepository {
        private readonly ChainRicContext _context;

        public RicRepository(IUnitOfWork<ChainRicContext> uow) {
            _context = uow.Context;
        }

        public RicRepository() {
            _context = new ChainRicContext();
        }

        public IQueryable<Ric> FindAll() {
            return _context.Rics;
        }

        public IQueryable<Ric> FindAllIncluding(params Expression<Func<Ric, object>>[] inc) {
            return inc.Aggregate<Expression<Func<Ric, object>>, IQueryable<Ric>>(
                _context.Rics,
                (current, expression) => current.Include(expression));
        }

        public IQueryable<Ric> FindBy(Func<Ric, bool> predicate) {
            return _context.Rics.Where(predicate).AsQueryable();
        }

        public Ric FindById(long id) {
            return _context.Rics.Find(id);
        }

        public void Insert(Ric item) {
            _context.Rics.Add(item);
            if (item.State != State.Added)
                _context.ApplyStateChanges();
        }

        public void Add(Ric item) {
            _context.Entry(item).State = item.id == default(long) ?
                EntityState.Added :
                EntityState.Modified;
        }

        public void Remove(Ric item) {
            _context.Rics.Remove(item);
        }

        public void Dispose() {
            _context.Dispose();
        }
    }

    public interface IRicToChainRepository : IRepository<RicToChain> {}
    public interface IIndexRepository : IRepository<Index> {}
    public interface IOrdinaryFrnRepository : IReadOnlyRepository<OrdinaryFrn> {}

    public class OrdinaryFrnRepository : ReadOnlyRepository<EnumerationsContext>, IOrdinaryFrnRepository {
        public IQueryable<OrdinaryFrn> FindAll() {
            return Context.OrdinaryFrn;
        }

        public IQueryable<OrdinaryFrn> FindAllIncluding(params Expression<Func<OrdinaryFrn, object>>[] inc) {
            return inc.Aggregate<Expression<Func<OrdinaryFrn, object>>, IQueryable<OrdinaryFrn>>(
                Context.OrdinaryFrn,
                (current, expression) => current.Include(expression));
        }

        public IQueryable<OrdinaryFrn> FindBy(Func<OrdinaryFrn, bool> predicate) {
            return Context.OrdinaryFrn.Where(predicate).AsQueryable();
        }

        public OrdinaryFrn FindById(long id) {
            return Context.OrdinaryFrn.Find(id);
        }
    }
}