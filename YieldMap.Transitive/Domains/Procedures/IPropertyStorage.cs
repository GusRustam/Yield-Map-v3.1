using System.Collections.Generic;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IPropertyStorage {
        void Save(Dictionary<long, Dictionary<long, string>> values);
    }
}
