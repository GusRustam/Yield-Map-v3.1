using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NRatingToInstrument : IIdentifyable, IEquatable<NRatingToInstrument> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Rating { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_Instrument { get; set; }
        
        [DbField(3)] // ReSharper disable once InconsistentNaming
        public DateTime? RatingDate { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NRatingToInstrument other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return id_Rating == other.id_Rating && id_Instrument == other.id_Instrument && RatingDate == other.RatingDate;
        }
    }
}