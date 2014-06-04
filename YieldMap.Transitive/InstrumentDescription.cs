using System;
using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class InstrumentDescription {
        public string Ric{ get; private set; }
        public string Isin{ get; private set; }
        public string Series{ get; private set; }

        public string ShortName{ get; private set; }
        public string IssuerName{ get; private set; }
        public string BorrowerName{ get; private set; }
        public string IssuerCountry{ get; private set; }
        public string BorrowerCountry{ get; private set; }

        public string Currency{ get; private set; }

        public long? IssueSize{ get; private set; }
        public string RateStructure{ get; private set; }

        public string Ticker{ get; private set; }
        public string ParentTicker{ get; private set; }
        public string Industry{ get; private set; }
        public string SubIndustry{ get; private set; }
        public string Seniority{ get; private set; }
        public string Specimen{ get; private set; }

        public DateTime? Issue{ get; private set; }
        public DateTime? Maturity{ get; private set; }
        public DateTime? NextCoupon{ get; private set; }

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
            RateStructure = bond.RateStructure;

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