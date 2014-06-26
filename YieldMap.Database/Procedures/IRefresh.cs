using System;

namespace YieldMap.Database.Procedures {
    public interface IRefresh {
        bool NeedsReload(DateTime dt);
        Chain[] ChainsInNeed(DateTime dt);

        /// <summary>
        /// Enumerates rics of all EXISTING bonds that need to be refreshed
        /// Condition for refresh is presense of embedded option which is due in +/- 7 days from today
        /// irrespective to if these rics come from chains or they are standalone
        /// </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        Ric[] StaleBondRics(DateTime dt);

        Ric[] AllBondRics();

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        Ric[] ObsoleteBondRics(DateTime dt);
    }
}