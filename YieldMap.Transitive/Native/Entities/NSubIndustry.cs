using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NSubIndustry : IIdName, IEquatable<NSubIndustry> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2)] // Resharper disable InconsistentNaming once
        public long id_Industry { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NSubIndustry other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Industry == other.id_Industry;
        }
    }
}