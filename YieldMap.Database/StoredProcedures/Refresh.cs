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

    public static class DateExtension {
        public static bool InRange(this DateTime pivot, DateTime what, int range) {
            
        }
    }

    public static class Refresh {
        private static readonly string ConnStr;
        static Refresh() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static bool NeedsUpdate(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                var needsChains = ctx.Chains.Any(c => c.Expanded.HasValue && c.Expanded.Value < dt);
                var needsRics = ctx.InstrumentBonds.Any(b => b.LastOptionDate.HasValue && b.LastOptionDate.Value.InRange(dt, 7));
                return needsChains || needsRics;
            }
        }

        public static IEnumerable<RicInfo> RicsToUpdate {
            get {
                return null;
            }
        }

        public static IEnumerable<RicInfo> StandaloneRics {
            get {
                using (var ctx = new MainEntities(ConnStr)) {
                    var rics = from r in ctx.Rics 
                               where !r.RicToChains.Any() && r.Feed != null
                               select new RicInfo {Ric = r.Name, Feed = r.Feed.Name};

                    return rics.ToList();
                }
            }
        }

        public static IEnumerable<ChainInfo> Chains {
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
