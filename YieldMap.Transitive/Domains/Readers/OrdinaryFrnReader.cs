using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Readers {
    public class OrdinaryFrnReader : ReadOnlyRepository<EnumerationsContext>, IOrdinaryFrnReader {
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