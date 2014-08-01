using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NFilter : IIdentifyable, IEquatable<NFilter> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public string Expression { get; set; }

        public bool Equals(NFilter other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && Expression == other.Expression;
        }
    }
}