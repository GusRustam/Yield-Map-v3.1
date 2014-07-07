using System;
using System.Linq;
using System.Linq.Expressions;

namespace YieldMap.Transitive.Domains {
    public interface IReadOnlyRepository<T> : IDisposable {
        IQueryable<T> FindAll();
        IQueryable<T> FindAllIncluding(params Expression<Func<T, object>>[] inc);
        IQueryable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);
    }
}