using System;
using System.Collections.Generic;

namespace YieldMap.Database.StoredProcedures {
    public interface IChainsLogic {
        Dictionary<Mission, string[]> Classify( DateTime dt, string[] chainRics);
    }
}