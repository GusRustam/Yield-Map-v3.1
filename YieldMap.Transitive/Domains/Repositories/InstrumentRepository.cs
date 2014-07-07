using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
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

        public int Insert(Instrument item) {
            _context.Instruments.Add(item);
            if (item.State != State.Added)
                _context.ApplyStateChanges();
            return 0;
        }

        public int Add(Instrument item) {
            _context.Entry(item).State = item.id == default(long) ?
                EntityState.Added :
                EntityState.Modified;
            return 0;
        }

        public int Remove(Instrument item) {
            _context.Instruments.Remove(item);
            return 0;
        }

        public void Dispose() {
            _context.Dispose();
        }
    }
}