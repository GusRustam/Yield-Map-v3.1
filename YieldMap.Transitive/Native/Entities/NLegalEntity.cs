using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NLegalEntity : IIdName, IEquatable<NLegalEntity> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_Country { get; set; }

        public bool Equals(NLegalEntity other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Country == other.id_Country;
        }
    }
}