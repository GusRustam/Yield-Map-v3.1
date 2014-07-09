using System;

namespace YieldMap.Transitive.Procedures {
    public interface IChainRics {
        void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms);
    }
}