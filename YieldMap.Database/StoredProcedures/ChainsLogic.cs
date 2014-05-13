using System;
using System.Collections.Generic;
using System.Linq;

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

            var existing = new HashSet<Ric>();
            var obsolete = new HashSet<Ric>();
            var toReload = new HashSet<Ric>();

            existing = existing.Add(Refresh.AllBondRics());
            obsolete = obsolete.Add(Refresh.ObsoleteBondRics(dt));  // obsolete done!
            toReload = toReload.Add(Refresh.StaleBondRics(dt));     // new rics to be added!
            
            var keep = existing.Remove(toReload).Remove(obsolete);

            IEnumerable<string> collection = null;
            try {
                collection = existing.Select(ric => ric.Name);
            } catch (NullReferenceException e) {
                Console.WriteLine(e);
            }
            var existingNames = new HashSet<string>(collection);
            var toRleoadNames = new HashSet<string>(toReload.Select(ric => ric.Name));

            var hsChainRics = new HashSet<string>(chainRics);

            var newRics = hsChainRics.Remove(existingNames);
            toRleoadNames = toRleoadNames.Add(newRics);

            res[Mission.Obsolete] = obsolete.Select(ric => ric.Name).ToArray();
            res[Mission.Keep] = keep.Select(ric => ric.Name).ToArray();
            res[Mission.ToReload] = toRleoadNames.ToArray();

            return res;
        }
    }
}