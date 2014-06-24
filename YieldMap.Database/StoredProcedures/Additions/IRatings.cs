using System.Collections.Generic;

namespace YieldMap.Database.StoredProcedures.Additions {
    public interface IRatings {
        void SaveRatings(IEnumerable<Transitive.Rating> ratings);
    }
}