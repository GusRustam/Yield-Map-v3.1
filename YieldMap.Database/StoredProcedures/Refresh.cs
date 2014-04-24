using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    public struct RicInfo {
        public string Ric;
        public string Feed;
    }

    public struct ChainInfo {
        public string ChainRic;
        public string Feed;
        public string Params;
    }

    public static class Extensions {
        public static bool InRange(this DateTime pivot, DateTime what, int range) {
            return what - pivot <= TimeSpan.FromDays(range);
        }

        public static bool NeedsRefresh(this Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today;
        }

        public static bool NeedsRefresh(this InstrumentBond bond, DateTime today) {
            return bond.LastOptionDate.HasValue && bond.LastOptionDate.Value.InRange(today, 7);  //todo why 7?
        }
    }

    public static class Refresh {
        private static readonly string ConnStr;
        static Refresh() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        private static IEnumerable<Chain> ChainsInNeed(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from c in ctx.Chains where c.NeedsRefresh(dt) select c;
            }
        }

        private static IEnumerable<Ric> RicsInNeed(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds where b.NeedsRefresh(dt) select b.Ric;
            }
        }

        public static bool NeedsUpdate(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                var needsChains = ctx.Chains.Any(c => c.NeedsRefresh(dt));
                var needsRics = ctx.InstrumentBonds.Any(b => b.NeedsRefresh(dt));
                return needsChains || needsRics;
            }
        }

        public static IEnumerable<RicInfo> RicsToReload(DateTime dt) {
            return 
                (from c in ChainsInNeed(dt) select c.Rics)                      // all rics from chains that should be loaded
                .Aggregate((agg, next) => agg.Concat(next).ToList())            // aggregating into single array
                .Concat(RicsInNeed(dt))                                         // adding separate rics
                .Distinct()                                                     // removing duplicates
                .Select(x => new RicInfo {Feed = x.Feed.Name, Ric = x.Name});   // creating RicInfo records
        }

        public static IEnumerable<RicInfo> StandaloneRicsToLoad {
            get {
                using (var ctx = new MainEntities(ConnStr)) {
                    var rics = from r in ctx.Rics 
                               where !r.Chains.Any() && r.Feed != null
                               select new RicInfo {Ric = r.Name, Feed = r.Feed.Name};

                    return rics.ToList();
                }
            }
        }

        public static IEnumerable<ChainInfo> ChainsToLoad {
            get {
                using (var ctx = new MainEntities(ConnStr)) {
                    var chains = from c in ctx.Chains
                                 where c.Feed != null
                                 select new ChainInfo {ChainRic = c.Name, Feed = c.Feed.Name, Params = c.Params};
                    return chains.ToList();
                }
            }
        }
    }
}