using System.Collections.Generic;

namespace YieldMap.Database.Procedures.Additions {
    public interface IRatings {
        void SaveRatings(IEnumerable<Transitive.Rating> ratings);
    }
}