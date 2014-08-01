using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NFilterToDescription : IIdentifyable, IEquatable<NFilterToDescription> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Filter { get; set; } 

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long id_Description { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public bool includes { get; set; }

        public bool Equals(NFilterToDescription other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return id_Filter == other.id_Filter && includes == other.includes &&
                   id_Description == other.id_Description;
        }
    }
}