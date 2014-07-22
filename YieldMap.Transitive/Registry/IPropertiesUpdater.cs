using System;
using YieldMap.Database;

namespace YieldMap.Transitive.Registry {
    /// <summary>
    /// Provides tools to recalculate properties for given instruments
    /// </summary>
    public interface IPropertiesUpdater {
        /// <summary>
        /// Evaluates all registered properties against all instruments stored in database
        /// Saves all values calculated
        /// </summary>
        /// <returns>0 if success</returns>
        int RecalculateBonds(Func<BondDescriptionView, bool> predicate = null);

        ///// <summary>
        ///// Refreshes current function registry
        ///// </summary>
        ///// <returns></returns>
        //int Refresh();
    }
}