using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
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
}