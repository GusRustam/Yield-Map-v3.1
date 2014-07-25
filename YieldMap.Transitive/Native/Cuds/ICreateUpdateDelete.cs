using System;

namespace YieldMap.Transitive.Native.Cuds {
    public interface ICreateUpdateDelete<in T> where T : IEquatable<T> {
        void Create(T item);
        void Update(T item);
        void Delete(T item);

        void Save();
    }
}
