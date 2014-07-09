using System;
using YieldMap.Database;

namespace YieldMap.Transitive.Registry {
    public interface IPropertiesUpdater {
        /// <summary>
        /// Evaluates all registered properties against all instruments stored in database
        /// Saves all values calculated
        /// </summary>
        /// <returns>0 if success</returns>
        int Recalculate(Func<InstrumentDescriptionView, bool> predicate = null);

        /// <summary>
        /// Refreshes current function registry
        /// </summary>
        /// <returns></returns>
        int Refresh();
    }
}