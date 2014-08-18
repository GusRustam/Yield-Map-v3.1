using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NIdx : IIdName, IEquatable<NIdx> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2)] // Resharper disable InconsistentNaming once
        public long? id_Ric { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NIdx other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Ric == other.id_Ric;
        }
    }
}