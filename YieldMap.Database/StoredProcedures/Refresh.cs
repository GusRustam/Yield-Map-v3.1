using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    static class Extensions {
        public static bool InRange(this DateTime pivot, DateTime what, int range) {
            return what - pivot <= TimeSpan.FromDays(range);
        }

        public static bool NeedsRefresh(this Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today;
        }

        public static bool NeedsRefresh(this InstrumentBond bond, DateTime today) {
            return bond.NextCoupon.HasValue && bond.NextCoupon.Value.InRange(today, 7);  //todo why 7?
        }

        public static HashSet<T> Add<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Add(item));
            return res;
        }

        public static HashSet<T> Remove<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Remove(item));
            return res;
        }
    }

    public static class Refresh {
        private static readonly string ConnStr;
        static Refresh() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static IEnumerable<Chain> ChainsInNeed(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from c in ctx.Chains where c.NeedsRefresh(dt) select c;
            }
        }

        public static bool NeedsReload(DateTime dt) {
            return ChainsInNeed(dt).Any();
        }

        /// <summary>
        /// Enumerates rics of all EXISTING bonds that need to be refreshed
        /// Condition for refresh is presense of embedded option which is due in +/- 7 days from today
        /// irrespective to if these rics come from chains or they are standalone
        /// </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static IEnumerable<Ric> StaleBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds where b.NeedsRefresh(dt) select b.Ric;
            }
        }

        public static IEnumerable<Ric> AllBondRics() {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds select b.Ric;
            }
        }

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static IEnumerable<Ric> ObsoleteBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds 
                       where b.Maturity.HasValue && b.Maturity.Value < dt 
                       select b.Ric;
            }
        }
    }
}