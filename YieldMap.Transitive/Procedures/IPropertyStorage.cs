using System.Collections.Generic;

namespace YieldMap.Transitive.Procedures {
    public interface IPropertyStorage {
        void Save(Dictionary<long, Dictionary<long, string>> values);
    }
}
