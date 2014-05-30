using System;
using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class FrnTransitive {
        public string Ric { get; set; }
        public string Isin { get; set; }
        
        public string FrnStructure { get; set; }
        public string RateStructure { get; set; }
        
        public long? IssueSize { get; set; }
        
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string IssuerName { get; set; }
        public string BorrowerName { get; set; }
        
        public DateTime? Issue { get; set; }
        public DateTime? Maturity { get; set; }
        
        public string Currency { get; set; }
        public string Ticker { get; set; }
        public string ParentTicker { get; set; }
        public string Series { get; set; }
        public string IssuerCountry { get; set; }
        public string BorrowerCountry { get; set; }
        public string Seniority { get; set; }
        public string Industry { get; set; }
        public string SubIndustry { get; set; }
        public string Instrument { get; set; }
        
        public DateTime? NextCoupon { get; set; }

        public string IndexName { get; set; }
        public float? Cap { get; set; }
        public float? Floor { get; set; }
        public float? Margin { get; set; }

        public static FrnTransitive CreateFrn(MetaTables.FrnData frn, MetaTables.BondDescr bond) {
            return null; // TODO NOW CREATE SUCH FRN VIA METABOND AND THEN FEED IT INTO DB STORER
            // TODO SIMILAR THING TO DO WITH ISSUE/ISSUER RATING
        }
    }
}
