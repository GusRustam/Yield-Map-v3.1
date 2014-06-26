using System;
using System.Collections.Generic;

namespace YieldMap.Database.Procedures {
    public interface IChainsLogic {
        Dictionary<Mission, string[]> Classify( DateTime dt, string[] chainRics);
    }
}