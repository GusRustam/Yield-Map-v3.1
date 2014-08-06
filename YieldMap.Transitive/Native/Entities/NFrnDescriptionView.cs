using YieldMap.Transitive.Native.Variables;

namespace YieldMap.Transitive.Native.Entities {
    public class NFrnDescriptionView : NInstrumentDescriptionView {
        [DbField(26, "BondStructure")] // ReSharper disable once InconsistentNaming
        public string FrnStructure { get; set; }

        [DbField(27), Variable] // ReSharper disable once InconsistentNaming
        public double? Margin { get; set; }

        [DbField(28, "IdxName"), Variable] // ReSharper disable once InconsistentNaming
        public string IndexName { get; set; }

        [DbField(29, "IdxRic"), Variable] // ReSharper disable InconsistentNaming once
        public string IndexRic { get; set; }

        public NFrnDescriptionView() {
        }

        public NFrnDescriptionView(NInstrumentDescriptionView parent, string bondStructure, double? margin, string indexName, string indexRic) {
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
            FrnStructure = bondStructure;
            Margin = margin;
            IndexName = indexName;
            IndexRic = indexRic;
        }
    }
}