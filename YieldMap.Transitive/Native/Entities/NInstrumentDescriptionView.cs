using System;
using YieldMap.Transitive.Native.Variables;

namespace YieldMap.Transitive.Native.Entities {
    public class NInstrumentDescriptionView : ITypedInstrument {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }

        [DbField(1), Variable("Name")] // ReSharper disable once InconsistentNaming
        public string InstrumentName { get; set; }

        [DbField(2), Variable("InstrumentType")] // ReSharper disable once InconsistentNaming
        public string InstrumentTypeName { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long id_InstrumentType { get; set; }

        [DbField(4), Variable] // ReSharper disable once InconsistentNaming
        public long? IssueSize { get; set; }

        [DbField(5), Variable("Series")] // ReSharper disable InconsistentNaming
        public string Series { get; set; }

        [DbField(6), Variable("IssueDate")] // ReSharper disable InconsistentNaming
        public DateTime? Issue { get; set; }

        [DbField(7), Variable] // ReSharper disable InconsistentNaming
        public DateTime? Maturity { get; set; }

        [DbField(8), Variable] // ReSharper disable InconsistentNaming
        public DateTime? NextCoupon { get; set; }

        [DbField(9), Variable("Ticker")] // ReSharper disable InconsistentNaming
        public string TickerName { get; set; }

        [DbField(10), Variable("SubIndustry")] // ReSharper disable InconsistentNaming
        public string SubIndustryName { get; set; }

        [DbField(11), Variable("Industry")] // ReSharper disable InconsistentNaming
        public string IndustryName { get; set; }

        [DbField(12), Variable("Specimen")] // ReSharper disable InconsistentNaming
        public string SpecimenName { get; set; }

        [DbField(13), Variable("Seniority")] // ReSharper disable InconsistentNaming
        public string SeniorityName { get; set; }

        [DbField(14), Variable("Ric")] // ReSharper disable InconsistentNaming
        public string RicName { get; set; }

        [DbField(15), Variable("Isin")] // ReSharper disable InconsistentNaming
        public string IsinName { get; set; }

        [DbField(16), Variable("Borrower.Name")] // ReSharper disable InconsistentNaming
        public string BorrowerName { get; set; }

        [DbField(17), Variable("Borrower.Country")] // ReSharper disable InconsistentNaming
        public string BorrowerCountryName { get; set; }

        [DbField(18), Variable("Issuer.Name")] // ReSharper disable InconsistentNaming
        public string IssuerName { get; set; }

        [DbField(19), Variable("Issuer.Country")] // ReSharper disable InconsistentNaming
        public string IssuerCountryName { get; set; }

        [DbField(20), Variable("Instrument.Rating")] // ReSharper disable InconsistentNaming
        public string InstrumentRating { get; set; }

        [DbField(21), Variable("Instrument.RatingDate")] // ReSharper disable InconsistentNaming
        public DateTime? InstrumentRatingDate { get; set; }

        [DbField(22), Variable("Instrument.RatingAgency")] // ReSharper disable InconsistentNaming
        public string InstrumentRatingAgency { get; set; }

        [DbField(23), Variable("Issuer.Rating")] // ReSharper disable InconsistentNaming
        public string IssuerRating { get; set; }

        [DbField(24), Variable("Issuer.RatingDate")] // ReSharper disable InconsistentNaming
        public DateTime? IssuerRatingDate { get; set; }

        [DbField(25), Variable("Issuer.RatingAgency")] // ReSharper disable InconsistentNaming
        public string IssuerRatingAgency { get; set; }
    }
}