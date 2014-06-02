using System;
using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class InstrumentDescription {
        public string Ric;
        public string Isin;
        public string Series;

        public string ShortName;
        public string IssuerName;
        public string BorrowerName;
        public string IssuerCountry;
        public string BorrowerCountry;

        public string Currency;

        public long? IssueSize;
        public string RateStructure;

        public string Ticker;
        public string ParentTicker;
        public string Industry;
        public string SubIndustry;
        public string Seniority;
        public string Specimen;

        public DateTime? Issue;
        public DateTime? Maturity;
        public DateTime? NextCoupon;

        protected InstrumentDescription(MetaTables.BondDescr bond) {
            Ric = bond.Ric;
            Isin = bond.Isin;
            Series = bond.Series;

            ShortName = bond.ShortName;

            IssuerName = bond.IssuerName;
            BorrowerName = bond.BorrowerName;
            IssuerCountry = bond.IssuerCountry;
            BorrowerCountry = bond.BorrowerCountry;

            Currency = bond.Currency;

            IssueSize = bond.IssueSize;
            RateStructure = bond.BondStructure;

            Ticker = bond.Ticker;
            ParentTicker = bond.ParentTicker;
            Industry = bond.Industry;
            SubIndustry = bond.SubIndustry;
            Seniority = bond.Seniority;
            Specimen = bond.Instrument;

            Issue = bond.Issue;
            Maturity = bond.Maturity;
            NextCoupon = bond.NextCoupon;
        }
    }
}