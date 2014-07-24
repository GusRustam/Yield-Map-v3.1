using System.Collections.Generic;

namespace YieldMap.Transitive.Tools {
    public interface ISet<T> : IEnumerable<T> {
        Set<T> Add(IEnumerable<T> items);
        Set<T> Add(T item);
        Set<T> Union(Set<T> another);
        Set<T> Intersect(Set<T> another);
        Set<T> Subtract(Set<T> another);
        Set<T> SymmetricDiff(Set<T> another);
        bool Contains(T id);
    }
}