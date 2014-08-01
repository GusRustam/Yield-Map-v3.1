using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NSourceType : IIdentifyable, IEquatable<NSourceType> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }

        public bool Equals(NSourceType other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name;
        }
    }
}