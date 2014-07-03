using System;
using System.Linq;
using System.Linq.Expressions;

namespace YieldMap.Transitive.Domains {
    public interface IRepository<T> : IDisposable {
        IQueryable<T> FindAll();
        IQueryable<T> FindAllIncluding(params Expression<Func<T, object>>[] inc);
        IQueryable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);

        /// <summary>
        /// Inserts explicitly, updates all graph
        /// </summary>
        /// <param name="item">item in question</param>
        void Insert(T item);

        /// <summary>
        /// Marks item as added or inserted
        /// </summary>
        /// <param name="item">item in question</param>
        void Add(T item);

        void Remove(T item);
    }
}
