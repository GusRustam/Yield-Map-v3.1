using System;
using YieldMap.Transitive.Native;

namespace YieldMap.Transitive.Procedures {
    public interface INewFunctionUpdater {
        int Recalculate<TItem>(Func<TItem, bool> predicate = null)
            where TItem : class, ITypedInstrument; // reader reads these items from the view
    }
}