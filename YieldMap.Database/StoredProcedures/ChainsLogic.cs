using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database.Tools;

namespace YieldMap.Database.StoredProcedures {
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
    public static class ChainsLogic {
        public static Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var r = new Refresh();

            var existing = new Set<String>(r.AllBondRics().Select(x => x.Name));
            var obsolete = new Set<String>(r.ObsoleteBondRics(dt).Select(x => x.Name));
            var toReload = new Set<String>(r.StaleBondRics(dt).Select(x => x.Name));
            var incoming = new Set<String>(chainRics);

            res[Mission.ToReload] = (incoming - existing + toReload).ToArray();
            res[Mission.Obsolete] = obsolete.ToArray();
            res[Mission.Keep] = (existing - obsolete - toReload).ToArray();

            return res;
        }
    }
}