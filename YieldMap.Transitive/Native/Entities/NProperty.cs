using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NProperty : IIdentifyable, IEquatable<NProperty> {
        public long id { get; set; }

        [DbField(1)]
        public string Name { get; set; }
        
        [DbField(2)]
        public string Description { get; set; }

        [DbField(3)]
        public string Expression { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public long id_InstrumentTpe { get; set; }
        
        public bool Equals(NProperty other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Name == other.Name && Description == other.Description &&
                   Expression == other.Expression && id_InstrumentTpe == other.id_InstrumentTpe;
        }
    }
}