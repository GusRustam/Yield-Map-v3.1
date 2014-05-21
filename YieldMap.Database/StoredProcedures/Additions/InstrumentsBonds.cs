using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using YieldMap.Database.Access;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class InstrumentsBonds : AccessToDb, IDisposable {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Additions.InstrumentsBonds");

        public void SaveFrns(IEnumerable<MetaTables.FrnData> bonds) {
        }

        public void SaveIssueRatings(IEnumerable<MetaTables.IssueRatingData> bonds) {
        }

        public void SaveIssuerRatings(IEnumerable<MetaTables.IssuerRatingData> bonds) {
        }

        public IEnumerable<Tuple<MetaTables.BondDescr, Exception>> SaveBonds(IEnumerable<MetaTables.BondDescr> bonds, bool useEf = false) {
            var res = new List<Tuple<MetaTables.BondDescr, Exception>>();
            var theBonds = bonds as IList<MetaTables.BondDescr> ?? bonds.ToList();


            // A kind of performance optimization.
            try {
                Context.Configuration.AutoDetectChangesEnabled = false;
                var isins = new Dictionary<string, Isin>();
                foreach (var bond in theBonds) {
                    var ric = Context.Rics.First(r => r.Name == bond.Ric);
                    if (!isins.ContainsKey(bond.Isin)) { 
                        var isin = EnsureIsin(Context, ric, bond.Isin);
                        isins.Add(bond.Isin, isin);
                    } else {
                        Logger.Warn(string.Format("Duplicate ISIN {0}", bond.Isin));
                        if (ric.Isin == null) ric.Isin = isins[bond.Isin];
                    }
                }
                Context.SaveChanges();
            } catch (DataException e) {
                Logger.ErrorEx("Saving isings failed", e);
            } finally {
                Context.Configuration.AutoDetectChangesEnabled = true;
            }

            var bondsToBeAdded = new Dictionary<string, InstrumentBond>();
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
                    var issuerCountry = EnsureCountry(Context, bond.IssuerCountry);
                    var borrowerCountry = EnsureCountry(Context, bond.BorrowerCountry);

                    instrument.Issuer = EnsureIssuer(Context, bond.IssuerName, issuerCountry);
                    instrument.Borrower = EnsureBorrower(Context, bond.BorrowerName, borrowerCountry);
                    instrument.Currency = EnsureCurrency(Context, bond.Currency);
                    instrument.Ticker = EnsureTicker(Context, bond.Ticker, bond.ParentTicker);
                    instrument.Seniority = EnsureSeniority(Context, bond.Seniority);
                    instrument.SubIndustry = EnsureSubIndustry(Context, bond.Industry, bond.SubIndustry);
                    instrument.Specimen = EnsureSpecimen(Context, bond.Instrument);

                    // CONSTRAINT: there already must be some ric with some feed!!!
                    instrument.Ric = Context.Rics.First(r => r.Name == bond.Ric);
                    instrument.Isin = instrument.Ric.Isin;

                } catch (Exception e) {
                    failed = true;
                    res.Add(Tuple.Create(bond, e));
                }
                if (!failed) {
                    if (!useEf) bondsToBeAdded.Add(instrument.Ric.Name, instrument); // NO ADDING OV BOND ITSELF HERE
                    else Context.InstrumentBonds.Add(instrument);
                }
                try {
                    Context.SaveChanges();
                } catch (DbEntityValidationException e) {
                    Logger.Report("Saving bonds failed", e);
                    throw;
                }
            }

            //CREATE TABLE InstrumentBond (
            // id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
            // id_Issuer       integer,
            // id_Borrower     integer,
            // id_Currency     integer,
            // BondStructure   text,
            // RateStructure   text,
            // IssueSize       integer,
            // Name            varchar(50) NOT NULL,
            // IsCallable      bit,
            // IsPutable       bit,
            // Series          varchar(50),
            // id_Isin         integer,
            // id_Ric          integer,
            // id_Ticker       integer,
            // id_SubIndustry  integer,
            // id_Specimen     integer,
            // Issue           date,
            // Maturity        date,
            // id_Seniority    integer,
            // NextCoupon      date,
            // Coupon          float(50)

            if (!useEf) {
                var bondsList = bondsToBeAdded.Values.ToList();
                var length = bondsList.Count();
                var iteration = 0;
                bool finished;

                do {
                    var minRange = iteration*500;
                    finished = minRange + 500 > length;
                    var maxRange = (finished ? length : minRange + 500) - 1;

                    var subList = bondsList.GetRange(minRange, maxRange - minRange + 1);
                    var sql =
                            subList.Aggregate(
                                "INSERT INTO InstrumentBond(" +
                                "id_Issuer, id_Borrower, id_Currency, id_Isin, id_Ric, id_ticker, id_SubIndustry, id_Specimen, id_Seniority, " +
                                "BondStructure, RateStructure, IssueSize, Name, IsCallable, IsPutable, Series, Issue, Maturity, NextCoupon, Coupon" +
                                ") VALUES",
                                (current, i) => {
                                    if (String.IsNullOrWhiteSpace(i.BondStructure))
                                        return current;

                                    var issueSize = i.IssueSize.HasValue ? i.IssueSize.Value : -1;
                                    var name = i.Name.Replace("''", "\"").Replace("'", "\"");
                                    var series = i.Series.Replace("''", "\"").Replace("'", "\"");
                                    var isCallable = i.IsCallable.HasValue && i.IsCallable.Value ? 1 : 0;
                                    var isPutable = i.IsPutable.HasValue && i.IsPutable.Value ? 1 : 0;

                                    var issue = i.Issue.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.Issue.Value.ToLocalTime()) : "NULL";
                                    var maturity = i.Maturity.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.Maturity.Value.ToLocalTime()) : "NULL";
                                    var nextCoupon = i.NextCoupon.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.NextCoupon.Value.ToLocalTime()) : "NULL";
                                    var coupon = i.Coupon.HasValue ? String.Format("'{0}'", i.Coupon.Value) : "0";

                                    var idIssuer = i.Issuer != null ? i.Issuer.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idBorrower = i.Borrower != null ? i.Borrower.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idCurrency = i.Currency != null ? i.Currency.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idIsin = i.Isin != null ? i.Isin.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idTicker = i.Ticker != null ? i.Ticker.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idSubIndustry = i.SubIndustry != null ? i.SubIndustry.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idSpecimen = i.Specimen != null ? i.Specimen.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idSeniority = i.Seniority != null ? i.Seniority.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                                    var idRic = i.Ric != null ? i.Ric.id.ToString(CultureInfo.InvariantCulture) : "NULL";

                                    return current +
                                        String.Format(
                                            "(" +
                                                "{0}, {1}, {2}, {3}, {19}, {4}, {5}, {6}, {7}, " +
                                                "'{8}', '{9}', {10}, '{11}', {12}, {13}, '{14}', " +
                                                "{15}, {16}, {17}, {18}" +
                                            "), ",
                                            idIssuer, idBorrower, idCurrency, idIsin, idTicker, idSubIndustry, idSpecimen,
                                            idSeniority,
                                            i.BondStructure, i.RateStructure, issueSize, name, isCallable, isPutable, series,
                                            issue, maturity, nextCoupon, coupon, idRic);
                                });

                    sql = sql.Substring(0, sql.Length - 2);
                    Logger.Info(String.Format("Sql is {0}", sql));

                    Context.Database.ExecuteSqlCommand(sql);
                    iteration = iteration + 1;
                } while (!finished);
                
            }
            return res;
        }

        private static Isin EnsureIsin(MainEntities ctx, Ric ric, string name) {
            if (String.IsNullOrWhiteSpace(name)) return null;

            var isin = ctx.Isins.FirstOrDefault(i => i.Name == name);
            if (isin != null) {
                if (isin.Feed == null)
                    isin.Feed = ric.Feed;
            } else {
                isin = ctx.Isins.Add(new Isin { Name = name, Feed = ric.Feed });
            }
            ric.Isin = isin;
            return isin;
        }

        private readonly Dictionary<string, Seniority> _seniorities = new Dictionary<string, Seniority>();
        private readonly Dictionary<string, Currency> _currencies = new Dictionary<string, Currency>();
        private readonly Dictionary<string, Borrower> _borrowers = new Dictionary<string, Borrower>();
        private readonly Dictionary<string, Issuer> _issuers = new Dictionary<string, Issuer>();
        private readonly Dictionary<string, Country> _countries = new Dictionary<string, Country>();
        private readonly Dictionary<string, Ticker> _tickers = new Dictionary<string, Ticker>();
        private readonly Dictionary<string, Industry> _industries = new Dictionary<string, Industry>();
        private readonly Dictionary<string, SubIndustry> _subIndustries = new Dictionary<string, SubIndustry>();
        private readonly Dictionary<string, Specimen> _specimens = new Dictionary<string, Specimen>();

        void IDisposable.Dispose() {
            // don't drop that durka durk
            _countries.Clear();
            _borrowers.Clear();
            _issuers.Clear();
            _seniorities.Clear();
            _tickers.Clear();
            _currencies.Clear();
            _industries.Clear();
            _subIndustries.Clear();
            _specimens.Clear();
        }

        private SubIndustry EnsureSubIndustry(MainEntities ctx, string ind, string sub) {
            if (String.IsNullOrWhiteSpace(ind))
                return null;
            
            var industry = EnsureIndustry(ctx, ind);

            var subIndustry = _subIndustries.ContainsKey(sub) ?_subIndustries[sub] :
                ctx.SubIndustries.FirstOrDefault(t => t.Name == sub) ??
                ctx.SubIndustries.Add(new SubIndustry { Name = sub });
            
            subIndustry.Industry = industry;
            
            _subIndustries[sub] = subIndustry;
            return subIndustry;
        }

        private Specimen EnsureSpecimen(MainEntities ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;
            
            if (_specimens.ContainsKey(name))
                return _specimens[name];

            var pt = ctx.Specimens.FirstOrDefault(t => t.name == name) ??
                     ctx.Specimens.Add(new Specimen { name = name });

            _specimens[name] = pt;
            return pt;
        }

        private Industry EnsureIndustry(MainEntities ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            if (_industries.ContainsKey(name))
                return _industries[name];

            var industry = ctx.Industries.FirstOrDefault(t => t.Name == name) ??
                           ctx.Industries.Add(new Industry {Name = name});

            _industries[name] = industry;
            return industry;
        }

        private Seniority EnsureSeniority(MainEntities ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            if (_seniorities.ContainsKey(name))
                return _seniorities[name];

            var pt = ctx.Seniorities.FirstOrDefault(t => t.Name == name) ??
                     ctx.Seniorities.Add(new Seniority {Name = name});

            _seniorities[name] = pt;
            return pt;
        }

        private Ticker EnsureTicker(MainEntities ctx, string child, string parent) {
            if (String.IsNullOrWhiteSpace(child))
                return null;
            
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
            if (String.IsNullOrWhiteSpace(name))
                return null;

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
            if (String.IsNullOrWhiteSpace(name))
                return null;

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
            if (String.IsNullOrWhiteSpace(name))
                return null;

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
            if (String.IsNullOrWhiteSpace(name)) 
                return null;

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