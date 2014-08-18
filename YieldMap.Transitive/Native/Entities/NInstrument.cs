using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NInstrument : IIdentifyable, IEquatable<NInstrument> {
        [DbField(0)]
        public long id { get; set; }
        
        [DbField(1)]
        public string Name { get; set; }
        
        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_InstrumentType { get; set; }


        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? id_Description { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NInstrument other) {
            if (other == null) return false;
            if (id != default(long) && other.id != default(long) && id == other.id) return true;
            return Name == other.Name && id_InstrumentType == other.id_InstrumentType &&
                   id_Description == other.id_Description;
        }
    }
}