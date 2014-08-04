using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NOrdinaryBond : INotIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }
        
        [DbField(1)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }

        [DbField(2)] // ReSharper disable InconsistentNaming
        public string Series { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? IssueSize { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public string RateStructure { get; set; }

        [DbField(5)] // ReSharper disable once InconsistentNaming
        public string BondStructure { get; set; }

        [DbField(6)] // ReSharper disable once InconsistentNaming
        public long id_Isin { get; set; }

        [DbField(7)] // ReSharper disable once InconsistentNaming
        public long id_Ric { get; set; }

        [DbField(8)] // ReSharper disable once InconsistentNaming
        public string Isin { get; set; }

        [DbField(9)] // ReSharper disable once InconsistentNaming
        public string Ric { get; set; }

        [DbField(10)] // ReSharper disable once InconsistentNaming
        public long? id_Ticker { get; set; }

        [DbField(11)] // ReSharper disable once InconsistentNaming
        public long? id_SubIndustry { get; set; }

        [DbField(12)] // ReSharper disable once InconsistentNaming
        public long? id_Specimen { get; set; }

        [DbField(13)] // ReSharper disable once InconsistentNaming
        public long? id_Seniority { get; set; }

        [DbField(14)] // ReSharper disable InconsistentNaming
        public DateTime? Issue { get; set; }

        [DbField(15)] // ReSharper disable InconsistentNaming
        public DateTime? Maturity { get; set; }

        [DbField(16)] // ReSharper disable InconsistentNaming
        public DateTime? NextCoupon { get; set; }

        [DbField(17)] // ReSharper disable InconsistentNaming
        public double? Coupon { get; set; }

        [DbField(18)] // ReSharper disable once InconsistentNaming
        public long? id_Currency { get; set; }
    }
}