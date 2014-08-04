using System;
using System.Collections.Generic;
using YieldMap.Database;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Procedures {
    /// <summary>
    /// Determines whether th Db needs to be updated and 
    /// classifies the existing rics into classes - Stale, Obsolete and Ok
    /// Stale are those that need refresh, obsolete 
    /// have to be deleted and Ok should be kept
    /// </summary>
    public interface IDbUpdates {
        bool NeedsReload(DateTime dt);
        IEnumerable<NChain> ChainsInNeed(DateTime dt);

        /// <summary>
        /// Enumerates rics of all EXISTING bonds that need to be refreshed
        /// Condition for refresh is presense of embedded option which is due in +/- 7 days from today
        /// irrespective to if these rics come from chains or they are standalone
        /// </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        IEnumerable<NRic> StaleBondRics(DateTime dt);

        IEnumerable<NRic> AllBondRics();

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        IEnumerable<NRic> ObsoleteBondRics(DateTime dt);

        Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics);
    }
}