using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Repositories {
    public class PropertiesRepository : IPropertiesRepository {
        private readonly PropertiesContext _context;

        public PropertiesRepository(IUnitOfWork<PropertiesContext> uow) {
            _context = uow.Context;
        }

        public PropertiesRepository() {
            _context = new PropertiesContext();
        }

        public IQueryable<Property> FindAll() {
            return _context.Properties;
        }

        public IQueryable<Property> FindAllIncluding(params Expression<Func<Property, object>>[] inc) {
            return inc.Aggregate<Expression<Func<Property, object>>, IQueryable<Property>>(
                _context.Properties, 
                (current, expression) => current.Include(expression));
        }

        public IQueryable<Property> FindBy(Func<Property, bool> predicate) {
            return _context.Properties.Where(predicate).AsQueryable();
        }

        public Property FindById(long id) {
            return _context.Properties.Find(id);
        }

        public int Insert(Property item) {
            _context.Properties.Add(item);
            if (item.State != State.Added) 
                _context.ApplyStateChanges();
            return 0;
        }

        public int Add(Property item) {
            _context.Entry(item).State = item.id == default(long) ? 
                EntityState.Added : 
                EntityState.Modified;
            return 0;
        }

        public int Remove(Property item) {
            _context.Properties.Remove(item);
            return 0;
        }

        public void Dispose() {
            _context.Dispose();
        }
    }
}