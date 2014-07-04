using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using Autofac;
using YieldMap.Database;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Enums;
using YieldMap.Transitive.MediatorTypes;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive.Domains.Procedures {
    internal class Bonds : IBonds {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Database.Additions.Bonds");
        private readonly ILegTypes _legTypes;
        private readonly IInstrumentTypes _instrumentTypes;

        public Bonds(IContainer container) {
            _legTypes = container.Resolve<ILegTypes>();
            _instrumentTypes = container.Resolve<IInstrumentTypes>();
        }

        private Leg CreateLeg(BondAdditionContext ctx, long descrId, InstrumentDescription description) {
            Leg leg = null;
            if (description is Bond) {
                var bond = description as Bond;
                leg = new Leg {
                    Structure = bond.BondStructure,
                    FixedRate = bond.Coupon,
                    Currency = EnsureCurrency(ctx, bond.Currency),
                    id_LegType = _legTypes.Received.id,
                    id_Instrument = ctx.Instruments.First(i => i.id_Description == descrId).id
                };
            } else if (description is Frn) {
                var note = description as Frn;
                leg  = new Leg {
                    Structure = note.FrnStructure,
                    Index = EnsureIndex(ctx, note.IndexName),
                    Cap = note.Cap,
                    Floor = note.Floor,
                    Margin = note.Margin,
                    Currency = EnsureCurrency(ctx, note.Currency),
                    id_LegType = _legTypes.Received.id,
                    id_Instrument = ctx.Instruments.First(i => i.id_Description == descrId).id
                };
            }
            if (leg != null) return leg;
            throw new InvalidDataException();
        }

        private static Index EnsureIndex(BondAdditionContext ctx, string ind) {
            if (String.IsNullOrWhiteSpace(ind))
                return null;

            var index =
                ctx.Indices.FirstOrDefault(t => t.Name == ind) ??
                ctx.Indices.Add(new Index { Name = ind });

            return index;
        }

        public void Save(IEnumerable<InstrumentDescription> bonds) {
            bonds = bonds as IList<InstrumentDescription> ?? bonds.ToList();

            // Creating ISINs, and linking RICs to them
            using (var ctx = new EikonEntitiesContext()) {
                try {
                    var isins = new Dictionary<string, Isin>();
                    foreach (var bond in bonds) {
                        if (bond.RateStructure.StartsWith("Unable"))
                            continue;

                        var ric = ctx.Rics.First(r => r.Name == bond.Ric);
                        if (!isins.ContainsKey(bond.Isin)) {
                            var isin = EnsureIsin(ctx, ric, bond.Isin);
                            isins.Add(bond.Isin, isin);
                        } else {
                            Logger.Warn(string.Format("Duplicate ISIN {0}", bond.Isin));
                            if (ric.Isin == null) ric.Isin = isins[bond.Isin];
                        }
                    }
                    try {
                        ctx.SaveChanges();  // maybe I could disable AutoDetectChagnes and then mark rics manually as updated
                    } catch (DbEntityValidationException e) {
                        Logger.Report("Saving rics/isins failed", e);
                        throw;
                    }
                } catch (DataException e) {
                    Logger.ErrorEx("Saving isings failed", e);
                } 
            }

            // Descriptions
            using (var ctx = new BondAdditionContext()) {
                var descriptions = new Dictionary<string, Description>();
                foreach (var bond in bonds) {
                    if (bond.RateStructure.StartsWith("Unable"))
                        continue;

                    Description description = null;
                    var failed = false;
                    try {

                        description = new Description {
                            RateStructure = bond.RateStructure,
                            IssueSize = bond.IssueSize,
                            Series = bond.Series,
                            Issue = bond.Issue,
                            Maturity = bond.Maturity,
                            NextCoupon = bond.NextCoupon
                        };

                        // Handling issuer, borrower and their countries
                        var issuerCountry = EnsureCountry(ctx, bond.IssuerCountry);
                        var borrowerCountry = EnsureCountry(ctx, bond.BorrowerCountry);

                        description.Issuer = EnsureLegalEntity(ctx, bond.IssuerName, issuerCountry);
                        description.Borrower = EnsureLegalEntity(ctx, bond.BorrowerName, borrowerCountry);
                        description.Ticker = EnsureTicker(ctx, bond.Ticker, bond.ParentTicker);
                        description.Seniority = EnsureSeniority(ctx, bond.Seniority);
                        description.SubIndustry = EnsureSubIndustry(ctx, bond.Industry, bond.SubIndustry);
                        description.Specimen = EnsureSpecimen(ctx, bond.Specimen);

                        // CONSTRAINT: there already must be some ric with some feed!!!
                        description.Ric = ctx.Rics.First(r => r.Name == bond.Ric);
                        description.Isin = description.Ric.Isin;
                    } catch (Exception e) {
                        failed = true;
                        Logger.ErrorEx("Instrument", e);
                    }
                    if (failed) continue;
                    descriptions.Add(bond.Ric, description);
                        
                }
                try {
                    ctx.SaveChanges();   // Saving all new records in tables Country, LegalEntity etc
                } catch (DbEntityValidationException e) {
                    Logger.Report("Saving descriptions failed", e);
                    throw;
                }
                if (descriptions.Any()) {
                    var peggedContext = ctx;
                    descriptions.Values.ChunkedForEach(x => {
                        var sql = BulkInsertBondDescription(x);
                        Logger.Info(String.Format("Sql is {0}", sql));
                        peggedContext.Database.ExecuteSqlCommand(sql);
                    }, 500);
                }
            }

            // Creating instruments
            var descrIds = new Dictionary<string, long>(); // ric -> id
            using (var ctx = new BondAdditionContext()) {
                var instruments = new Dictionary<string, Instrument>();
                foreach (var bond in bonds) {
                    if (bond.RateStructure.StartsWith("Unable"))
                        continue;

                    Instrument instrument = null;
                    var failed = false;
                    try {
                        descrIds[bond.Ric] = ctx.Descriptions.First(d => d.Ric.Name == bond.Ric).id;

                        instrument = new Instrument {
                            Name = bond.ShortName,
                            id_InstrumentType = _instrumentTypes.Bond.id,
                            id_Description = descrIds[bond.Ric]  
                        };
                    } catch (Exception e) {
                        failed = true;
                        Logger.ErrorEx("Instrument", e);
                    }
                    if (failed) continue;
                    instruments.Add(bond.Ric, instrument);
                }

                if (instruments.Any()) {
                    var peggedContext = ctx;
                    instruments.Values.ChunkedForEach(x => {
                        var sql = BulkInsertInstruments(x);
                        Logger.Info(String.Format("Sql is {0}", sql));
                        peggedContext.Database.ExecuteSqlCommand(sql);
                    }, 500);
                }
            }


            // Legs
            using (var ctx = new BondAdditionContext()) {
                try {
                    ctx.Configuration.AutoDetectChangesEnabled = false;
                    var legs = new Dictionary<string, Leg>();
                    foreach (var bond in bonds.Where(bond => !bond.RateStructure.StartsWith("Unable"))) {
                        try {
                            legs.Add(bond.Ric, CreateLeg(ctx, descrIds[bond.Ric], bond));
                        } catch (Exception e) {
                            Logger.ErrorEx("Instrument", e);
                        }
                    }

                    if (legs.Any()) {
                        try {
                            ctx.SaveChanges();
                        } catch (DbEntityValidationException e) {
                            Logger.Report("Saving legs failed", e);
                            throw;
                        }
                        
                        var peggedContext = ctx;
                        legs.Values.ChunkedForEach(x => {
                            var sql = BulkInsertLegs(x);
                            Logger.Info(String.Format("Sql is {0}", sql));
                            peggedContext.Database.ExecuteSqlCommand(sql);
                        }, 500);
                    }
                } finally {
                    ctx.Configuration.AutoDetectChangesEnabled = true;
                }
            }
        }

        private static string BulkInsertLegs(IEnumerable<Leg> legs) {
            var res =  legs.Aggregate(
                "INSERT INTO Leg(id_Instrument, id_LegType, id_Currency, id_Index, Structure, FixedRate, Cap, Floor, Margin) VALUES",
                (current, i) => {
                    var coupon = i.FixedRate.HasValue ? String.Format("'{0}'", i.FixedRate.Value) : "NULL";
                    var idIndex = i.Index != null ? i.Index.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var cap = i.Cap.HasValue ? i.Cap.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var floor = i.Floor.HasValue ? i.Floor.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var margin = i.Margin.HasValue ? i.Margin.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    return current + String.Format(
                        "({0}, {1}, {2}, {3}, '{4}', {5}, {6}, {7}, {8}), ", 
                        i.id_Instrument, i.id_LegType, i.Currency.id, idIndex, i.Structure, coupon, cap, floor, margin);
                });
            return res.Substring(0, res.Length - 2);
        }

        private static string BulkInsertInstruments(IEnumerable<Instrument> instruments) {
            var res = instruments.Aggregate(
                "INSERT INTO Instrument(id_InstrumentType, id_Description, Name) SELECT ",
                (current, i) => {
                    var name = i.Name.Replace("'","");
                    return current + String.Format("{0}, {1}, '{2}' UNION SELECT ", i.id_InstrumentType, i.id_Description, name);
                });
            return res.Substring(0, res.Length - "UNION SELECT ".Length);
        }

        private static string BulkInsertBondDescription(IEnumerable<Description> descriptions) {
            var res = descriptions.Aggregate(
                "INSERT INTO Description(" +
                "id_Issuer, id_Borrower, id_Isin, id_Ric, id_Ticker, id_SubIndustry, id_Specimen, id_Seniority, " +
                "RateStructure, IssueSize, Series, Issue, Maturity, NextCoupon" +
                ") SELECT ",
                (current, i) => {
                    var issueSize = i.IssueSize.HasValue ? i.IssueSize.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var series = i.Series.Replace("''", "\"").Replace("'", "\"");

                    var issue = i.Issue.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.Issue.Value.ToLocalTime()) : "NULL";
                    var maturity = i.Maturity.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.Maturity.Value.ToLocalTime()) : "NULL";
                    var nextCoupon = i.NextCoupon.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.NextCoupon.Value.ToLocalTime()) : "NULL";

                    var idIssuer = i.Issuer != null ? i.Issuer.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idBorrower = i.Borrower != null ? i.Borrower.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idIsin = i.Isin != null ? i.Isin.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idTicker = i.Ticker != null ? i.Ticker.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idSubIndustry = i.SubIndustry != null ? i.SubIndustry.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idSpecimen = i.Specimen != null ? i.Specimen.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idSeniority = i.Seniority != null ? i.Seniority.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idRic = i.Ric != null ? i.Ric.id.ToString(CultureInfo.InvariantCulture) : "NULL";

                    return current + String.Format("{0}, {1}, {2}, {13}, {3}, {4}, {5}, {6}, '{7}', {8}, '{9}', {10}, {11}, {12} UNION SELECT ",
                                                  idIssuer, idBorrower, idIsin, idTicker, idSubIndustry, idSpecimen, idSeniority, 
                                                  i.RateStructure, issueSize, series, issue, maturity, nextCoupon, idRic);
                });
            return res.Substring(0, res.Length - "UNION SELECT ".Length);
        }

        private static Isin EnsureIsin(EikonEntitiesContext ctx, Ric ric, string name) {
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
        private readonly Dictionary<string, LegalEntity> _legalEntities = new Dictionary<string, LegalEntity>();
        private readonly Dictionary<string, Country> _countries = new Dictionary<string, Country>();
        private readonly Dictionary<string, Ticker> _tickers = new Dictionary<string, Ticker>();
        private readonly Dictionary<string, Industry> _industries = new Dictionary<string, Industry>();
        private readonly Dictionary<string, SubIndustry> _subIndustries = new Dictionary<string, SubIndustry>();
        private readonly Dictionary<string, Specimen> _specimens = new Dictionary<string, Specimen>();

        private SubIndustry EnsureSubIndustry(BondAdditionContext ctx, string ind, string sub) {
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

        private Specimen EnsureSpecimen(BondAdditionContext ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;
            
            if (_specimens.ContainsKey(name))
                return _specimens[name];

            var pt = ctx.Specimens.FirstOrDefault(t => t.name == name) ??
                     ctx.Specimens.Add(new Specimen { name = name });

            _specimens[name] = pt;
            return pt;
        }

        private Industry EnsureIndustry(BondAdditionContext ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            if (_industries.ContainsKey(name))
                return _industries[name];

            var industry = ctx.Industries.FirstOrDefault(t => t.Name == name) ??
                           ctx.Industries.Add(new Industry {Name = name});

            _industries[name] = industry;
            return industry;
        }

        private Seniority EnsureSeniority(BondAdditionContext ctx, string name) {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            if (_seniorities.ContainsKey(name))
                return _seniorities[name];

            var pt = ctx.Seniorities.FirstOrDefault(t => t.Name == name) ??
                     ctx.Seniorities.Add(new Seniority {Name = name});

            _seniorities[name] = pt;
            return pt;
        }

        private Ticker EnsureTicker(BondAdditionContext ctx, string child, string parent) {
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

        private Ticker EnsureParentTicker(BondAdditionContext ctx, string name) {
            if (String.IsNullOrWhiteSpace(name)) 
                return null;

            if (_tickers.ContainsKey(name))
                return _tickers[name];

            var pt = ctx.Tickers.FirstOrDefault(t => t.Name == name) ??
                     ctx.Tickers.Add(new Ticker { Name = name });

            _tickers[name] = pt;
            return pt;
        }

        private Currency EnsureCurrency(BondAdditionContext ctx, string name) {
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

        private LegalEntity EnsureLegalEntity(BondAdditionContext ctx, string name, Country country) {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            if (_legalEntities.ContainsKey(name))
                return _legalEntities[name];
            
            var legalEntity = ctx.LegalEntities.FirstOrDefault(b => b.Name == name);
            if (legalEntity != null) {
                _legalEntities[name] = legalEntity;
                return legalEntity;
            }

            legalEntity = ctx.LegalEntities.Add(new LegalEntity {Name = name, Country = country});
            _legalEntities[name] = legalEntity;
            return legalEntity;
        }

        private Country EnsureCountry(BondAdditionContext ctx, string name) {
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