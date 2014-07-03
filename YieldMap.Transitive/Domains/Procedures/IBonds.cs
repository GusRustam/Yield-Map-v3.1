using System.Collections.Generic;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IBonds {
        void Save(IEnumerable<InstrumentDescription> bonds);
    }
}