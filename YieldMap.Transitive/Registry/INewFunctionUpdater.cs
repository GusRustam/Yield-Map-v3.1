using System;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;

namespace YieldMap.Transitive.Registry {
    public interface INewFunctionUpdater {
        int Recalculate<TItem, TReader>(Func<TItem, bool> predicate = null)
            where TItem : class, ITypedInstrument // item is a record from view
            where TReader : IReader<TItem>; // reader reads these items from the view
    }
}