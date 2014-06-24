using System.Collections.Generic;
using YieldMap.Transitive;

namespace YieldMap.Database.StoredProcedures.Additions {
    public interface IBonds {
        void Save(IEnumerable<InstrumentDescription> bonds);
    }
}