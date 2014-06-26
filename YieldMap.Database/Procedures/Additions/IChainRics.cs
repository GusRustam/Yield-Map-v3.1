using System;
using System.Collections.Generic;

namespace YieldMap.Database.Procedures.Additions {
    public interface IChainRics {
        void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms);
        void DeleteRics(HashSet<string> rics);
    }
}