using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NFieldDefinition : IIdentifyable, IEquatable<NFieldDefinition> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NFieldDefinition other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name;
        }
    }
}