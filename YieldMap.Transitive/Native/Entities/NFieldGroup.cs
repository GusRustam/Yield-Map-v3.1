using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NFieldGroup : IIdentifyable, IEquatable<NFieldGroup> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2, "[Default]")]
        public bool Default { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? id_DefaultFieldDef { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NFieldGroup other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && Default == other.Default &&
                   id_DefaultFieldDef == other.id_DefaultFieldDef;
        }
    }
}