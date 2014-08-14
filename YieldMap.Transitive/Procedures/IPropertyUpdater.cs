using System.Collections.Generic;
using YieldMap.Transitive.Native;

namespace YieldMap.Transitive.Procedures {
    public interface IPropertyUpdater {
        int Recalculate<TItem>(IEnumerable<long> ids = null)
            where TItem : class, ITypedInstrument; // reader reads these items from the view

        int RecalculateAll(IEnumerable<long> ids = null);
    }
}