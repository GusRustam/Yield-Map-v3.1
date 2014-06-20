using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database.Access;
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
    internal static class ChainsLogic {
        public static Dictionary<Mission, string[]> Classify(this IDbConn conn, DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var existing = new Set<String>(conn.AllBondRics().Select(x => x.Name));
            var obsolete = new Set<String>(conn.ObsoleteBondRics(dt).Select(x => x.Name));
            var toReload = new Set<String>(conn.StaleBondRics(dt).Select(x => x.Name));
            var incoming = new Set<String>(chainRics);

            res[Mission.ToReload] = (incoming - existing + toReload).ToArray();
            res[Mission.Obsolete] = obsolete.ToArray();
            res[Mission.Keep] = (existing - obsolete - toReload).ToArray();

            return res;
        }
    }
}