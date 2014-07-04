using System.Collections.Generic;
using YieldMap.Transitive.MediatorTypes;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IBonds {
        void Save(IEnumerable<InstrumentDescription> bonds);
    }
}