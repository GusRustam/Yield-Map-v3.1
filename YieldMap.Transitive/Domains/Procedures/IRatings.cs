using System.Collections.Generic;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IRatings {
        void SaveRatings(IEnumerable<Rating> ratings);
    }
}