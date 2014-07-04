using System.Collections.Generic;
using YieldMap.Transitive.MediatorTypes;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IRatings {
        void SaveRatings(IEnumerable<Rating> ratings);
    }
}