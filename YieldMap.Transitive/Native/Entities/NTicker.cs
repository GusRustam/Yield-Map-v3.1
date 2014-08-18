using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NTicker : IIdName, IEquatable<NTicker> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2, "id_ParentTicker")] // ReSharper disable once InconsistentNaming
        public long? id_Parent { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NTicker other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Parent == other.id_Parent;
        }
    }
}