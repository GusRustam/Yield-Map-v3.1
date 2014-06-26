using System.Collections.Generic;
using YieldMap.Transitive;

namespace YieldMap.Database.Procedures.Additions {
    public interface IBonds {
        void Save(IEnumerable<InstrumentDescription> bonds);
    }
}