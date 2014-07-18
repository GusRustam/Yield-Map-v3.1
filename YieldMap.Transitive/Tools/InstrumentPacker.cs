using System.Collections.Generic;
using YieldMap.Database;

namespace YieldMap.Transitive.Tools {
    public class InstrumentPacker : IInstrumentPacker {
        public Dictionary<string, object> PackInstrumentDescription(InstrumentDescriptionView i) {
            var res = new Dictionary<string, object>();
            res["Name"] = i.IndustryName;
            res["Borrower.Country"] = i.BorrowerCountryName;
            res["Borrower.Name"] = i.BorrowerName;
            res["Issuer.Country"] = i.IssuerCountryName;
            res["Issuer.Name"] = i.IssuerName;
            res["Industry"] = i.IndustryName;
            res["Issue.Rating"] = i.InstrumentRating;
            res["Issue.RatingAgency"] = i.InstrumentRatingAgency;
            res["Issue.RatingDate"] = i.InstrumentRatingDate;
            res["Issuer.Rating"] = i.IssuerRating;
            res["Issuer.RatingAgency"] = i.IssuerRatingAgency;
            res["Issuer.RatingDate"] = i.IssuerRatingDate;
            res["Isin"] = i.IsinName;
            res["Ric"] = i.RicName;
            res["IssueDate"] = i.Issue;
            res["Maturity"] = i.Maturity;
            res["IssueSize"] = i.IssueSize;
            res["Industry"] = i.IndustryName;
            res["SubIndustry"] = i.SubIndustryName;
            res["Seniority"] = i.SeniorityName;
            res["Series"] = i.Series;
            res["Specimen"] = i.SpecimenName;
            res["Ticker"] = i.TickerName;
            return res;
        }
    }
}