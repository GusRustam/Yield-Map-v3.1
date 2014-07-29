using System;
using System.Collections.Generic;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public interface ICrud<T> where T : class, IIdentifyable, IEquatable<T> {
        void Create(T item);
        void Update(T item);
        void Delete(T item);

        IEnumerable<T> FindAll();
        IEnumerable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);

        void Save<U>() where U : class, IIdentifyable, IEquatable<U>;
    }
}
