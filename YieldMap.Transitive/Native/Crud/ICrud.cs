using System;
using System.Collections.Generic;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public interface ICrud<T> : IDisposable where T : class, IIdentifyable, IEquatable<T>  {
        void Create(T item);
        void Update(T item);
        void Delete(T item);
        void Save<TEntity>() where TEntity : class, IIdentifyable, IEquatable<TEntity>;

        /// <summary>
        /// Clears given table without any further questions
        /// Use with care!
        /// </summary>
        void DeleteAll();

        IEnumerable<T> FindAll();
        IEnumerable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);
    }
}
