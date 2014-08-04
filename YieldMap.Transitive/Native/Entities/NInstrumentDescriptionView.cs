using System;
using YieldMap.Transitive.Native.Variables;

namespace YieldMap.Transitive.Native.Entities {
    public class NInstrumentDescriptionView : ITypedInstrument {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public string InstrumentName { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public string InstrumentTypeName { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long id_InstrumentType { get; set; }

        [DbField(4), Variable] // ReSharper disable once InconsistentNaming
        public long? IssueSize { get; set; }

        [DbField(5)] // ReSharper disable InconsistentNaming
        public string Series { get; set; }

        [DbField(6)] // ReSharper disable InconsistentNaming
        public DateTime? Issue { get; set; }

        [DbField(7), Variable] // ReSharper disable InconsistentNaming
        public DateTime? Maturity { get; set; }

        [DbField(8), Variable] // ReSharper disable InconsistentNaming
        public DateTime? NextCoupon { get; set; }

        [DbField(9)] // ReSharper disable InconsistentNaming
        public string TickerName { get; set; }

        [DbField(10)] // ReSharper disable InconsistentNaming
        public string SubIndustryName { get; set; }

        [DbField(11)] // ReSharper disable InconsistentNaming
        public string IndustryName { get; set; }

        [DbField(12)] // ReSharper disable InconsistentNaming
        public string SpecimenName { get; set; }

        [DbField(13)] // ReSharper disable InconsistentNaming
        public string SeniorityName { get; set; }

        [DbField(14)] // ReSharper disable InconsistentNaming
        public string RicName { get; set; }

        [DbField(15)] // ReSharper disable InconsistentNaming
        public string IsinName { get; set; }

        [DbField(16)] // ReSharper disable InconsistentNaming
        public string BorrowerName { get; set; }

        [DbField(17)] // ReSharper disable InconsistentNaming
        public string BorrowerCountryName { get; set; }

        [DbField(18)] // ReSharper disable InconsistentNaming
        public string IssuerName { get; set; }

        [DbField(19)] // ReSharper disable InconsistentNaming
        public string IssuerCountryName { get; set; }

        [DbField(20)] // ReSharper disable InconsistentNaming
        public string InstrumentRating { get; set; }

        [DbField(21)] // ReSharper disable InconsistentNaming
        public DateTime? InstrumentRatingDate { get; set; }

        [DbField(22)] // ReSharper disable InconsistentNaming
        public string InstrumentRatingAgency { get; set; }

        [DbField(23)] // ReSharper disable InconsistentNaming
        public string IssuerRating { get; set; }

        [DbField(24)] // ReSharper disable InconsistentNaming
        public DateTime? IssuerRatingDate { get; set; }

        [DbField(25)] // ReSharper disable InconsistentNaming
        public string IssuerRatingAgency { get; set; }        
    }
}