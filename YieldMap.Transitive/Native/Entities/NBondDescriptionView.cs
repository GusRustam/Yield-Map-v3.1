using YieldMap.Transitive.Native.Variables;

namespace YieldMap.Transitive.Native.Entities {
    public class NBondDescriptionView : NInstrumentDescriptionView {
        [DbField(26)] // ReSharper disable once InconsistentNaming
        public string BondStructure { get; set; }

        [DbField(27), Variable("Coupon")] // ReSharper disable once InconsistentNaming
        public double? Coupon { get; set; }

        public NBondDescriptionView() {
        }

        public NBondDescriptionView(NInstrumentDescriptionView parent, string bondStructure, double? coupon) {
            id_Instrument = parent.id_Instrument; 
            InstrumentName = parent.InstrumentName;
            InstrumentTypeName = parent.InstrumentTypeName;
            id_InstrumentType = parent.id_InstrumentType;
            IssueSize = parent.IssueSize;
            Series = parent.Series;
            Issue = parent.Issue;
            Maturity = parent.Maturity;
            NextCoupon = parent.NextCoupon;
            TickerName = parent.TickerName;
            SubIndustryName = parent.SubIndustryName;
            IndustryName = parent.IndustryName;
            SpecimenName = parent.SpecimenName;
            SeniorityName = parent.SeniorityName;
            RicName = parent.RicName;
            IsinName = parent.IsinName;
            BorrowerName = parent.BorrowerName;
            BorrowerCountryName = parent.BorrowerCountryName;
            IssuerName = parent.IssuerName;
            IssuerCountryName = parent.IssuerCountryName;
            InstrumentRating = parent.InstrumentRating;
            InstrumentRatingDate = parent.InstrumentRatingDate;
            InstrumentRatingAgency = parent.InstrumentRatingAgency;
            IssuerRating = parent.IssuerRating;
            IssuerRatingDate = parent.IssuerRatingDate;
            IssuerRatingAgency = parent.IssuerRatingAgency;   
            BondStructure = bondStructure;
            Coupon = coupon;
        }
    }
}