using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NIsin : IIdentifyable, IEquatable<NIsin> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2)] // Resharper disable InconsistentNaming once
        public long? id_Feed { get; set; }

        public bool Equals(NIsin other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Feed == other.id_Feed;
        }
    }
}