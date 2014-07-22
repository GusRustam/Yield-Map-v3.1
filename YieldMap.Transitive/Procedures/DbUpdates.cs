using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Procedures {
    public class DbUpdates : ReadOnlyRepository<ChainRicContext>, IDbUpdates {
        private static bool NeedsRefresh(Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today || !chain.Expanded.HasValue;
        }

        private static bool NeedsRefresh(OrdinaryBond bond, DateTime today) {
            if (!bond.NextCoupon.HasValue)
                return false;

            var nextCoupon = bond.NextCoupon.Value;
            return nextCoupon < today + TimeSpan.FromDays(7);
        }

        public IEnumerable<Chain> ChainsInNeed(DateTime dt) {
            return (
                from c in Context.Chains.ToList()
                where NeedsRefresh(c, dt)
                select new Chain {
                    Name = c.Name,
                    Expanded = c.Expanded,
                    Feed = c.Feed == null ? null : new Feed {Name = c.Feed.Name}
                });
        }

        public bool NeedsReload(DateTime dt) {
            return ChainsInNeed(dt).Any();
        }

        public IEnumerable<Ric> StaleBondRics(DateTime dt) {
            return Context.OrdinaryBonds.ToList()
                .Where(b => b.Ric != null && NeedsRefresh(b, dt))
                .Select(b => Context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple());
        }

        public IEnumerable<Ric> AllBondRics() {
            return Context.OrdinaryBonds.ToList()
                .Where(b => b.Ric != null)
                .Select(b => Context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple());
        }

        public IEnumerable<Ric> ObsoleteBondRics(DateTime dt) {
            return Context.OrdinaryBonds.ToList()
                .Where(b => b.Ric != null && b.Maturity.HasValue && b.Maturity.Value < dt)
                .Select(b => Context.Rics.First(r => r.id == b.id_Ric).ToPocoSimple());
        }

        public Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var existing = new Set<String>(AllBondRics().Select(x => x.Name));
            var obsolete = new Set<String>(ObsoleteBondRics(dt).Select(x => x.Name));
            var toReload = new Set<String>(StaleBondRics(dt).Select(x => x.Name));
            var incoming = new Set<String>(chainRics);

            res[Mission.ToReload] = (incoming - existing + toReload).ToArray();
            res[Mission.Obsolete] = obsolete.ToArray();
            res[Mission.Keep] = (existing - obsolete - toReload).ToArray();

            return res;
        }
    }
}
