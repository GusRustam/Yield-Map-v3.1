using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    static class Extensions {
        public static bool InRange(this DateTime pivot, DateTime what, int range) {
            return what - pivot <= TimeSpan.FromDays(range);
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

        private static bool NeedsRefresh(Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today;
        }

        private static bool NeedsRefresh(this InstrumentBond bond, DateTime today) {
            return bond.NextCoupon.HasValue && bond.NextCoupon.Value.InRange(today, 7);  //todo why 7?
        }

        public static Chain[] ChainsInNeed(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return ctx.Chains.ToList().Where(c => NeedsRefresh(c, dt)).ToArray();
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
        public static Ric[] StaleBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return ctx
                    .InstrumentBonds
                    .ToList()
                    .Where(b => NeedsRefresh(b, dt))
                    .Select(b => b.Ric)
                    .ToArray();
            }
        }

        public static Ric[] AllBondRics() {
            using (var ctx = new MainEntities(ConnStr)) {
                return ctx
                    .InstrumentBonds
                    .ToList()
                    .Select(b => b.Ric)
                    .ToArray();
            }
        }

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static Ric[] ObsoleteBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return ctx
                    .InstrumentBonds
                    .Where(b => b.Maturity.HasValue && b.Maturity.Value < dt)
                    .Select(b => b.Ric)
                    .ToArray();
            }
        }
    }
}