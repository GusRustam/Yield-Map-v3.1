using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NRatingToLegalEntity : IIdentifyable, IEquatable<NRatingToLegalEntity> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Rating { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long id_LegalEntity { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public DateTime RatingDate { get; set; }

        public bool Equals(NRatingToLegalEntity other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return id_Rating == other.id_Rating && id_LegalEntity == other.id_LegalEntity && RatingDate == other.RatingDate;
        }
    }
}