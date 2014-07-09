using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Registry;

namespace YieldMap.Transitive.Repositories {
    public class PropertyValuesRepostiory : IPropertyValuesRepostiory {
        private readonly PropertiesContext _context;

        public PropertyValuesRepostiory(IPropertiesUnitOfWork uow) {
            _context = uow.Context;
        }

        public PropertyValuesRepostiory() {
            _context = new PropertiesContext();
        }
        
        public IQueryable<PropertyValue> FindAll() {
            return _context.PropertyValues;
        }

        public IQueryable<PropertyValue> FindAllIncluding(params Expression<Func<PropertyValue, object>>[] inc) {
            return inc.Aggregate<Expression<Func<PropertyValue, object>>, IQueryable<PropertyValue>>(
                _context.PropertyValues, 
                (current, expression) => current.Include(expression));
        }

        public IQueryable<PropertyValue> FindBy(Func<PropertyValue, bool> predicate) {
            return _context.PropertyValues.Where(predicate).AsQueryable();
        }

        public PropertyValue FindById(long id) {
            return _context.PropertyValues.Find(id);
        }

        public int Insert(PropertyValue item) {
            _context.PropertyValues.Add(item);
            if (item.State != State.Added) 
                _context.ApplyStateChanges();
            return 0;
        }

        public int Add(PropertyValue item) {
            _context.Entry(item).State = item.id == default(long) ? 
                EntityState.Added : 
                EntityState.Modified;
            return 0;
        }

        public int Remove(PropertyValue item) {
            _context.PropertyValues.Remove(item);
            return 0;
        }

        public void Dispose() {
            _context.Dispose();
        }
    }
}