using System;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;

namespace YieldMap.Transitive.Registry {
    public interface INewFunctionUpdater {
        int Recalculate<TItem>(Func<TItem, bool> predicate = null)
            where TItem : class, ITypedInstrument; // reader reads these items from the view
    }
}