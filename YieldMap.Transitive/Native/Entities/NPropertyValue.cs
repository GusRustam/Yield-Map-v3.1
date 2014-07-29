using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NPropertyValue : IIdentifyable, IEquatable<NPropertyValue> {
        [DbField(0)]
        public long id { get; set; }
        
        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Property { get; set; }
        
        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }
        
        [DbField(3)] 
        public string Value { get; set; }
        
        public bool Equals(NPropertyValue other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return id_Property == other.id_Property && id_Instrument == other.id_Instrument && Value == other.Value;
        }
    }
}