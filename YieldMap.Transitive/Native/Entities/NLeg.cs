using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NLeg : IIdentifyable, IEquatable<NLeg> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1)]
        public string Structure { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_Instrument { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? id_LegType { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public long? id_Currency { get; set; }

        [DbField(5)]
        public double? FixedRate { get; set; }

        [DbField(6, "id_Idx")] // ReSharper disable once InconsistentNaming
        public long? id_Index { get; set; }

        [DbField(7)]
        public double? Cap { get; set; }

        [DbField(8)]
        public double? Floor { get; set; }

        [DbField(9)]
        public double? Margin { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NLeg other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return Structure == other.Structure && id_Instrument == other.id_Instrument && id_LegType == other.id_LegType &&
                   id_Currency == other.id_Currency && FixedRate == other.FixedRate && id_Index == other.id_Index &&
                   Cap == other.Cap && Floor == other.Floor && Margin == other.Margin;
        }
    }
}