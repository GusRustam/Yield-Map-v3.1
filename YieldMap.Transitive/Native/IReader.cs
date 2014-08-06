using System;
using System.Collections.Generic;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native {
    public interface IReader<out T> where T : class, INotIdentifyable {
        IEnumerable<T> FindAll();
        IEnumerable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);
    }
}