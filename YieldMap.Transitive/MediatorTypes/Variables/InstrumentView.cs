using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class InstrumentView : IVariable {
        private readonly string _name;
        private readonly string _series;
        private readonly string _ric;
        private readonly string _isin;
        private readonly DateTime? _maturity;
        private readonly DateTime? _issue;
        private readonly long? _issueSize;
        private readonly string _industry;
        private readonly string _subIndustry;
        private readonly string _seniority;
        private readonly string _specimen;
        private readonly string _ticker;

        private readonly Entity _borrower;
        private readonly Issuer _issuer;
        private readonly RatingInfo _issueRating;

        public InstrumentView(dynamic i) {
            _name = i.InstrumentName;
            _series = i.Series;

            _isin = i.IsinName;
            _ric = i.RicName;
            _issue = i.Issue;
            _maturity = i.Maturity;
            _issueSize = i.IssueSize;

            _industry = i.IndustryName;
            _subIndustry = i.SubIndustryName;
            _seniority = i.SeniorityName;
            _specimen = i.SpecimenName;
            _ticker = i.TickerName;

            _borrower = new Entity {
                Country = i.BorrowerCountryName, 
                Name = i.BorrowerName
            };

            _issuer = new Issuer {
                Country = i.IssuerCountryName,
                Name = i.IssuerName,
                Rating = new RatingInfo {
                    Name = i.IssuerRating,
                    Agency = i.IssuerRatingAgency,
                    Date = i.IssuerRatingDate
                }
            };

            _issueRating = new RatingInfo {
                Name = i.InstrumentRating,
                Agency = i.InstrumentRatingAgency,
                Date = i.InstrumentRatingDate
            };
        }

        public virtual Dictionary<string, object> AsVariable() {
            var res = new Dictionary<string, object>();
            res["NAME"] = _name;
            res["SERIES"] = _series;
            res["ISIN"] = _isin;
            res["RIC"] = _ric;
            res["ISSUE"] = _issue;
            res["MATURITY"] = _maturity;
            res["ISSUESIZE"] = _issueSize;

            res["INDUSTRY"] = _industry;
            res["SUBINDUSTRY"] = _subIndustry;
            res["SENIORITY"] = _seniority;
            res["SPECIMEN"] = _specimen;
            res["TICKER"] = _ticker;

            res.VariableJoin(_issuer.AsVariable(), "ISSUER.");
            res.VariableJoin(_borrower.AsVariable(), "BORROWER.");
            res.VariableJoin(_issueRating.AsVariable(), "ISSUE.RATING");

            return res;
        }
    }
}

