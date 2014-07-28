using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace YieldMap.Transitive.Native.Crud {
    public interface ICrud<T> where T : IEquatable<T> {
        void Create(T item);
        void Update(T item);
        void Delete(T item);

        IEnumerable<T> FindAll();
        IEnumerable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);

        void Save();
    }
}
