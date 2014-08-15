using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NRicToChain : IIdentifyable, IEquatable<NRicToChain> {
        [DbField(0)]
        public long id { get; set; }

        [DbField(1, "Ric_id")] // ReSharper disable once InconsistentNaming
        public long id_Ric { get; set; }

        [DbField(2, "Chain_id")] // ReSharper disable once InconsistentNaming
        public long id_Chain { get; set; }

        public bool Equals(NRicToChain other) {
            if (other == null)
                return false;
            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;
            return id_Ric == other.id_Ric && id_Chain == other.id_Chain;
        }
    }
}