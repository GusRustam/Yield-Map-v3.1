using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using YieldMap.Requests.MetaTables;
//public void DeleteBonds(HashSet<string> names) {
//    using (var ctx = new MainEntities(DbConn.ConnectionString)) {
//        var rics = from ric in ctx.Rics
//                   where names.Contains(ric.Name)
//                   select ric;

//        // todo DELETING CAREFULLY!!!

//    }
//}
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class InstrumentsBonds : IDisposable {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Additions.InstrumentsBonds");

        public void SaveFrns(IEnumerable<MetaTables.FrnData> bonds) {
        }

        public void SaveIssueRatings(IEnumerable<MetaTables.IssueRatingData> bonds) {
        }

        public void SaveIssuerRatings(IEnumerable<MetaTables.IssuerRatingData> bonds) {
        }

        public IEnumerable<Tuple<MetaTables.BondDescr, Exception>> SaveBonds(IEnumerable<MetaTables.BondDescr> bonds) {
            var res = new List<Tuple<MetaTables.BondDescr, Exception>>();
            var theBonds = bonds as IList<MetaTables.BondDescr> ?? bonds.ToList();

            using (var ctx = new MainEntities(DbConn.ConnectionString)) {
                foreach (var bond in theBonds) {
                    InstrumentBond instrument = null;
                    var failed = false;
                    try {
                        instrument = new InstrumentBond {
                            BondStructure = bond.BondStructure,
                            RateStructure = bond.RateStructure,
                            IssueSize = bond.IssueSize,
                            Name = bond.ShortName,
                            IsCallable = bond.IsCallable,
                            IsPutable = bond.IsPutable,
                            Series = bond.Series,
                            Issue = bond.Issue,
                            Maturity = bond.Maturity,
                            Coupon = bond.Coupon,
                            NextCoupon = bond.NextCoupon
                        };

                        // Handling issuer, borrower and their countries
                        var issuerCountry = EnsureCountry(ctx, bond.IssuerCountry);
                        var borrowerCountry = EnsureCountry(ctx, bond.BorrowerCountry);

                        instrument.Issuer = EnsureIssuer(ctx, bond.IssuerName, issuerCountry);
                        instrument.Borrower = EnsureBorrower(ctx, bond.BorrowerName, borrowerCountry);
                        instrument.Currency = EnsureCurrency(ctx, bond.Currency);
                        instrument.Ticker = EnsureTicker(ctx, bond.Ticker, bond.ParentTicker);
                        instrument.Seniority = EnsureSeniority(ctx, bond.Seniority);
                        instrument.SubIndustry = EnsureSubIndustry(ctx, bond.Industry, bond.SubIndustry);
                        instrument.Specimen = EnsureSpecimen(ctx, bond.Instrument);

                        // CONSTRAINT: there already must be some ric with some feed!!!
                        instrument.Ric = ctx.Rics.First(r => r.Name == bond.Ric);
                        Logger.Info(string.Format("Adding bond with ric {0}, its id is {1}", bond.Ric, instrument.Ric.id));

                        instrument.Isin = EnsureIsin(ctx, bond.Isin, instrument.Ric);
                    } catch (Exception e) {
                        failed = true;
                        res.Add(Tuple.Create(bond, e));
                    }
                    if (!failed) ctx.InstrumentBonds.Add(instrument);
                }

                try {
                    ctx.SaveChanges();
                } catch (DbEntityValidationException e) {
                    Logger.Report(e);
                    throw;
                }
            }
            return res;
        }


        private readonly Dictionary<string, Seniority> _seniorities = new Dictionary<string, Seniority>();
        private readonly Dictionary<string, Currency> _currencies = new Dictionary<string, Currency>();
        private readonly Dictionary<string, Borrower> _borrowers = new Dictionary<string, Borrower>();
        private readonly Dictionary<string, Issuer> _issuers = new Dictionary<string, Issuer>();
        private readonly Dictionary<string, Country> _countries = new Dictionary<string, Country>();
        private readonly Dictionary<string, Ticker> _tickers = new Dictionary<string, Ticker>();
        private readonly Dictionary<string, Isin> _isins = new Dictionary<string, Isin>();
        private readonly Dictionary<string, Industry> _industries = new Dictionary<string, Industry>();
        private readonly Dictionary<string, SubIndustry> _subIndustries = new Dictionary<string, SubIndustry>();
        private readonly Dictionary<string, Specimen> _specimens = new Dictionary<string, Specimen>();

        public void Dispose() {
            // don't drop that durka durk
            _countries.Clear();
            _borrowers.Clear();
            _issuers.Clear();
            _seniorities.Clear();
            _tickers.Clear();
            _currencies.Clear();
            _isins.Clear();
            _industries.Clear();
            _subIndustries.Clear();
            _specimens.Clear();
        }

        private Isin EnsureIsin(MainEntities ctx, string name, Ric ric) {
            var isin = _isins.ContainsKey(name) ? _isins[name] :
                            ctx.Isins.FirstOrDefault(i => i.Name == name) ??
                            ctx.Isins.Add(new Isin { Name = name });
            
            isin.Feed = ric.Feed;
            _isins[name] = isin;
            return isin;
        }

        private SubIndustry EnsureSubIndustry(MainEntities ctx, string ind, string sub) {
            var industry = EnsureIndustry(ctx, ind);

            var subIndustry = _subIndustries.ContainsKey(sub) ?_subIndustries[sub] :
                ctx.SubIndustries.FirstOrDefault(t => t.Name == sub) ??
                ctx.SubIndustries.Add(new SubIndustry { Name = sub });
            
            subIndustry.Industry = industry;
            
            _subIndustries[sub] = subIndustry;
            return subIndustry;
        }

        private Specimen EnsureSpecimen(MainEntities ctx, string name) {
            if (_specimens.ContainsKey(name))
                return _specimens[name];

            var pt = ctx.Specimens.FirstOrDefault(t => t.name == name) ??
                     ctx.Specimens.Add(new Specimen { name = name });

            _specimens[name] = pt;
            return pt;
        }

        private Industry EnsureIndustry(MainEntities ctx, string ind) {
            if (_industries.ContainsKey(ind))
                return _industries[ind];

            var industry = ctx.Industries.FirstOrDefault(t => t.Name == ind) ??
                           ctx.Industries.Add(new Industry {Name = ind});

            _industries[ind] = industry;
            return industry;
        }

        private Seniority EnsureSeniority(MainEntities ctx, string name) {
            if (_seniorities.ContainsKey(name))
                return _seniorities[name];

            var pt = ctx.Seniorities.FirstOrDefault(t => t.Name == name) ??
                     ctx.Seniorities.Add(new Seniority {Name = name});

            _seniorities[name] = pt;
            return pt;
        }

        private Ticker EnsureTicker(MainEntities ctx, string child, string parent) {
            // There was situation when child and parent names were same. 
            // In this case parent is ignored
            var parentName = parent == child ? String.Empty : parent;
            var pt = EnsureParentTicker(ctx, parentName); // find or create the parent

            // Checking if there's already a ticker with such name
            Ticker ch;
            if (_tickers.ContainsKey(child)) {
                ch = _tickers[child];
                ch.Parent = pt;
            } else {
                ch = (pt == null 
                        ? ctx.Tickers.FirstOrDefault(t => t.Name == child && t.Parent == null) 
                        : ctx.Tickers.FirstOrDefault(t => t.Name == child && t.Parent != null && t.Parent.Name == pt.Name)) 
                     ?? ctx.Tickers.Add(new Ticker {Name = child, Parent = pt});

                _tickers[child] = ch; // store
            }

            return ch;
        }

        private Ticker EnsureParentTicker(MainEntities ctx, string name) {
            if (String.IsNullOrWhiteSpace(name)) 
                return null;

            if (_tickers.ContainsKey(name))
                return _tickers[name];

            var pt = ctx.Tickers.FirstOrDefault(t => t.Name == name) ??
                     ctx.Tickers.Add(new Ticker { Name = name });

            _tickers[name] = pt;
            return pt;
        }

        private Currency EnsureCurrency(MainEntities ctx, string name) {
            if (_currencies.ContainsKey(name))
                return _currencies[name];
            var currency = ctx.Currencies.FirstOrDefault(b => b.Name == name);
            if (currency != null) {
                _currencies[name] = currency;
                return currency;
            }

            currency = ctx.Currencies.Add(new Currency {Name = name});
            _currencies[name] = currency;
            return currency;
        }

        private Borrower EnsureBorrower(MainEntities ctx, string name, Country country) {
            if (_borrowers.ContainsKey(name))
                return _borrowers[name];
            
            var borrower = ctx.Borrowers.FirstOrDefault(b => b.Name == name);
            if (borrower != null) {
                _borrowers[name] = borrower;
                return borrower;
            }

            borrower = ctx.Borrowers.Add(new Borrower {Name = name, Country = country});
            _borrowers[name] = borrower;
            return borrower;
        }

        private Issuer EnsureIssuer(MainEntities ctx, string name, Country country) {
            if (_issuers.ContainsKey(name))
                return _issuers[name];
            var issuer = ctx.Issuers.FirstOrDefault(b => b.Name == name);
            if (issuer != null) {
                _issuers[name] = issuer;
                return issuer;
            }

            issuer = ctx.Issuers.Add(new Issuer {Name = name, Country = country});
            _issuers[name] = issuer;
            return issuer;
        }

        private Country EnsureCountry(MainEntities ctx, string name) {
            if (_countries.ContainsKey(name)) return _countries[name];

            var country = ctx.Countries.FirstOrDefault(c => c.Name == name);
            if (country != null) {
                _countries[name] = country;
                return country;
            }

            country = ctx.Countries.Add(new Country {Name = name});
            _countries[name] = country;
            return country;
        }
    }
}