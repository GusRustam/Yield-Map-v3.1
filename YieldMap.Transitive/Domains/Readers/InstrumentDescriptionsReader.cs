using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Readers {
    public class InstrumentDescriptionsReader : ReadOnlyRepository<InstrumentDescriptionContext>,
        IInstrumentDescriptionsReader {
        public InstrumentDescriptionsReader() {
        }

        public InstrumentDescriptionsReader(InstrumentDescriptionContext context) : base(context) {
        }

        public IQueryable<InstrumentDescriptionView> InstrumentDescriptionViews {
            get { return Context.InstrumentDescriptionViews.AsNoTracking(); }
        }

        public IQueryable<Instrument> Instruments {
            get { return Context.Instruments.AsNoTracking(); }
        }

        public IQueryable<Description> Descriptions {
            get { return Context.Descriptions.AsNoTracking(); }
        }

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