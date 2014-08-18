using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NRic : IIdentifyable, IEquatable<NRic> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }

        [DbField(2)] // ReSharper disable InconsistentNaming once
        public long? id_Isin { get; set; }

        [DbField(3)]
        public long? id_Feed { get; set; }

        [DbField(4)]
        public long? id_FieldGroup { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NRic other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && id_Feed == other.id_Feed && id_Isin == other.id_Isin &&
                   id_FieldGroup == other.id_FieldGroup;
        }
    }
}