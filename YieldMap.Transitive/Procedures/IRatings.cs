using System.Collections.Generic;
using YieldMap.Transitive.MediatorTypes;

namespace YieldMap.Transitive.Procedures {
    public interface IRatings {
        void Save(IEnumerable<Rating> ratings);
    }
}