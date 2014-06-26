using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database.Tools;

namespace YieldMap.Database.Procedures {
    /// Some considerations:
    /// 
    /// BondRics = All rics from Table InstrumentBond
    ///
    /// BondRics =
    /// |--> Obsolete (those who have no bond, or those who have matured)
    /// |--> ToReload (those who need reloading)
    /// |--> Keep     (others)
    /// 
    /// ChainRics
    /// |--> New       |--> ToReload
    /// |--> Existing  |--> ToReload
    ///                |--> Keep
    /// 
    /// 
    internal class ChainsLogic : IChainsLogic {
        private readonly IRefresh _refresh;

        public ChainsLogic(IRefresh refresh) {
            _refresh = refresh;
        }

        public Dictionary<Mission, string[]> Classify( DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var existing = new Set<String>(_refresh.AllBondRics().Select(x => x.Name));
            var obsolete = new Set<String>(_refresh.ObsoleteBondRics(dt).Select(x => x.Name));
            var toReload = new Set<String>(_refresh.StaleBondRics(dt).Select(x => x.Name));
            var incoming = new Set<String>(chainRics);

            res[Mission.ToReload] = (incoming - existing + toReload).ToArray();
            res[Mission.Obsolete] = obsolete.ToArray();
            res[Mission.Keep] = (existing - obsolete - toReload).ToArray();

            return res;
        }
    }
}