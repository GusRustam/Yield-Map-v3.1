using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NCurrency : IIdName, IEquatable<NCurrency> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        public bool Equals(NCurrency other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name;
        }
    }
}