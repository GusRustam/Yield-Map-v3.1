using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Procedures {
    public class DbUpdates : IDbUpdates {
        private readonly IContainer _container;

        public DbUpdates(Func<IContainer> containerF) {
            _container = containerF();
        }

        private static bool NeedsRefresh(NChain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today || !chain.Expanded.HasValue;
        }

        private static bool NeedsRefresh(NOrdinaryBond bond, DateTime today) {
            if (!bond.NextCoupon.HasValue)
                return false;

            var nextCoupon = bond.NextCoupon.Value;
            return nextCoupon < today + TimeSpan.FromDays(7);
        }

        public IEnumerable<NChain> ChainsInNeed(DateTime dt) {
            var chains = _container.Resolve<IChainCrud>();
            return (
                from c in chains.FindAll()
                where NeedsRefresh(c, dt)
                select new NChain {
                    Name = c.Name,
                    Expanded = c.Expanded,
                    id_Feed = c.id_Feed
                });
        }

        public bool NeedsReload(DateTime dt) {
            return ChainsInNeed(dt).Any();
        }

        public IEnumerable<NRic> StaleBondRics(DateTime dt) {
            var bonds = _container.Resolve<IReader<NOrdinaryBond>>();
            var rics = _container.Resolve<IRicCrud>();

            return bonds.FindAll()
                .Where(b => b.Ric != null && NeedsRefresh(b, dt))
                .Select(b => rics.FindById(b.id_Ric));
        }

        public IEnumerable<NRic> AllBondRics() {
            var bonds = _container.Resolve<IReader<NOrdinaryBond>>();
            var rics = _container.Resolve<IRicCrud>();

            return bonds.FindAll()
                .Where(b => b.Ric != null)
                .Select(b => rics.FindById(b.id_Ric));
        }

        public IEnumerable<NRic> ObsoleteBondRics(DateTime dt) {
            var bonds = _container.Resolve<IReader<NOrdinaryBond>>();
            var rics = _container.Resolve<IRicCrud>();

            return bonds.FindAll()
                .Where(b => b.Ric != null && b.Maturity.HasValue && b.Maturity.Value < dt)
                .Select(b => rics.FindById(b.id_Ric));
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
