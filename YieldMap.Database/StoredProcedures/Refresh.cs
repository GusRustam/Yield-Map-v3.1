using System;
using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures {
    internal static class Refresh {
        private static bool NeedsRefresh(Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today || !chain.Expanded.HasValue;
        }

        private static bool NeedsRefresh(OrdinaryBond bond, DateTime today) {
            if (!bond.NextCoupon.HasValue) return false;
            
            var nextCoupon = bond.NextCoupon.Value;
            return nextCoupon < today + TimeSpan.FromDays(7);
        }

        public static Chain[] ChainsInNeed(this IDbConn dbConn, DateTime dt) {
            return (from c in dbConn.CreateContext().Chains.ToList() 
                    where NeedsRefresh(c, dt)
                    select new Chain {
                        Name = c.Name, 
                        Expanded = c.Expanded, 
                        Feed = c.Feed == null ? null : new Feed {Name = c.Feed.Name}
                    }).ToArray();
        }

        public static bool NeedsReload(this IDbConn dbConn, DateTime dt) {
            return dbConn.ChainsInNeed(dt).Any();
        }

        /// <summary>
        /// Enumerates rics of all EXISTING bonds that need to be refreshed
        /// Condition for refresh is presense of embedded option which is due in +/- 7 days from today
        /// irrespective to if these rics come from chains or they are standalone
        /// </summary>
        /// <param name="dbConn"></param>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static Ric[] StaleBondRics(this IDbConn dbConn, DateTime dt) {
            var context = dbConn.CreateContext();
            return context.OrdinaryBonds.ToList()
                .Where(b => b.Ric != null && NeedsRefresh(b, dt))
                .Select(b => context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple())
                .ToArray();
        }

        public static Ric[] AllBondRics(this IDbConn dbConn) {
            var context = dbConn.CreateContext();
            return context.OrdinaryBonds.ToList()
                    .Where(b => b.Ric != null)
                    .Select(b => context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple())
                    .ToArray();
        }

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dbConn"></param>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static Ric[] ObsoleteBondRics(this IDbConn dbConn, DateTime dt) {
            var context = dbConn.CreateContext();
            return context.OrdinaryBonds.ToList()
                    .Where(b => b.Ric != null && b.Maturity.HasValue && b.Maturity.Value < dt)
                    .Select(b => context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple())
                    .ToArray();
        }
    }
}