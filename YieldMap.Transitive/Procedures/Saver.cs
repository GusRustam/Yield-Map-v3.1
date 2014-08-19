using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.MediatorTypes;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Tools;
using Rating = YieldMap.Transitive.MediatorTypes.Rating;

namespace YieldMap.Transitive.Procedures {
    public class Saver : ISaver {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Procedures.Saver");
        private readonly IFieldResolver _resolver;
        private readonly ILegTypes _legTypes;
        private readonly IInstrumentTypes _instrumentTypes;

        private readonly IContainer _container;
        private bool _notifications = true;
        private SQLiteConnection _connection;

        public event EventHandler<IDbEventArgs> Notify;
        public void DisableNotifications() {
            _notifications = false;
        }

        public void EnableNotifications() {
            _notifications = true;
        }

        public Saver(Func<IContainer> containerF) {
            _container = containerF.Invoke();
            _resolver = _container.Resolve<IFieldResolver>();
            _legTypes = _container.Resolve<ILegTypes>();
            _instrumentTypes = _container.Resolve<IInstrumentTypes>();
            _connection = _container.Resolve<IConnector>().GetConnection();
            _connection.Open();
        }

        public void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms) {
            if (prms == null)
                prms = string.Empty;

            try {
                var feedId = UpdateFeed(feedName);
                var chainId = UpdateChain(chainRic, feedId, expanded, prms);

                var ricToChains = _container.Resolve<ICrud<NRicToChain>>();
                var existingRicIds = new Set<long>(ricToChains.FindBy(t => t.id_Chain == chainId).Select(t => t.id));

                var ricsTable = _container.Resolve<ICrud<NRic>>();
                var existingRics = ricsTable.FindBy(r => existingRicIds.Contains(r.id)).Select(r => r.Name).ToArray();

                var newRics = new HashSet<string>(rics);
                newRics.RemoveWhere(existingRics.Contains);

                AddRics(chainId, feedId, newRics);

                // todo CHAINRIC REPORTING:
                //var chainsUpd = context.ExtractEntityChanges<Chain>();
                //var ricsUpd = context.ExtractEntityChanges<Ric>();

                //if (Notify != null & _notifications) {
                //    Notify(this, new DbEventArgs(chainsUpd.ExtractIds(), EventSource.Chain));
                //    Notify(this, new DbEventArgs(ricsUpd.ExtractIds(), EventSource.Ric));
                //}

            } catch (Exception e) {
                Logger.ErrorEx("Failed to save", e);
                throw;
            }
        }

        private long UpdateFeed(string feedName) {
            var feedTable = _container.ResolveCrudWithConnection<NFeed>(_connection);
            var feeds = feedTable.FindBy(f => f.Name == feedName).ToList();
            if (!feeds.Any()) {
                var feed = new NFeed {Name = feedName};
                feedTable.Create(feed);
                feedTable.Save();
                return feed.id;
            }
            return feeds.First().id;
        }

        public void SaveInstruments(IEnumerable<InstrumentDescription> bonds) {
            bonds = bonds as IList<InstrumentDescription> ?? bonds.ToList();
            bonds = bonds.Where(bond => !bond.RateStructure.StartsWith("Unable"));

            var seniorities1 = new Dictionary<string, long>();
            var legalEntities1 = new Dictionary<string, long>();
            var countries1 = new Dictionary<string, long>();
            var tickers1 = new Dictionary<string, long>();
            var industries1 = new Dictionary<string, long>();
            var indices1 = new Dictionary<string, long>();
            var subIndustries1 = new Dictionary<string, long>();
            var specimens1 = new Dictionary<string, long>();
            var currencies1 = new Dictionary<string, long>();

            // Creating ISINs, and linking RICs to them
            var isinTable = _container.ResolveCrudWithConnection<NIsin>(_connection);
            var allIsins = isinTable.FindAll().ToDictionary(n => n.Name, n => n);

            var ricTable = _container.ResolveCrudWithConnection<NRic>(_connection);
            var allRics = ricTable.FindAll().ToDictionary(r => r.Name, r => r);

            foreach (var bond in bonds.Where(b => !string.IsNullOrEmpty(b.Isin))) {
                var ric = allRics[bond.Ric]; // todo ric comparison??
                if (!allIsins.ContainsKey(bond.Isin)) {
                    var isin = new NIsin { Name = bond.Isin, id_Feed = ric.id_Feed };
                    isinTable.Create(isin);
                    allIsins.Add(bond.Isin, isin);
                }
            }
            isinTable.Save();

            foreach (var bond in bonds.Where(b => !string.IsNullOrEmpty(b.Isin))) {
                try {
                    var ric = allRics[bond.Ric]; // todo ric comparison??
                    if (!ric.id_Isin.HasValue) {
                        ric.id_Isin = allIsins[bond.Isin].id;
                        ricTable.Update(ric);
                        allRics[ric.Name] = ric;
                    }
                } catch (Exception e) {
                    Logger.ErrorEx("Saving isins failed", e);
                }
            }
            ricTable.Save();

            // Countries
            var countryTable = _container.ResolveCrudWithConnection<NCountry>(_connection);
            var countries = countryTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NCountry>();
            foreach (var bond in bonds) {
                if (!string.IsNullOrEmpty(bond.IssuerCountry))
                    SaveIdName(bond.IssuerCountry, countryTable, countries);
                if (!string.IsNullOrEmpty(bond.BorrowerCountry))
                    SaveIdName(bond.BorrowerCountry, countryTable, countries);
            }
            countryTable.Save();
            foreach (var nCountry in countries) 
                countries1[nCountry.Key] = nCountry.Value.id;

            // Legal Entites
            var legalEntitiesTable = _container.ResolveCrudWithConnection<NLegalEntity>(_connection);
            var legalEntities = legalEntitiesTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NLegalEntity>();
            foreach (var bond in bonds) {
                if (!string.IsNullOrEmpty(bond.IssuerName)) {
                    var idCountry = countries1.ContainsKey(bond.IssuerCountry) ? (long?)countries1[bond.IssuerCountry] : null;
                    SaveLegalEntity(bond.IssuerName, idCountry, legalEntitiesTable, legalEntities1, legalEntities);
                }
                if (!string.IsNullOrEmpty(bond.BorrowerName)) {
                    var country = countries1.ContainsKey(bond.BorrowerCountry) ? (long?)countries1[bond.BorrowerCountry] : null;
                    SaveLegalEntity(bond.BorrowerName, country, legalEntitiesTable, legalEntities1, legalEntities);
                }
            }
            legalEntitiesTable.Save();
            foreach (var legalEntity in legalEntities)
                legalEntities1[legalEntity.Key] = legalEntity.Value.id;

            // Tickers
            var tickerTable = _container.ResolveCrudWithConnection<NTicker>(_connection);
            var tickers = tickerTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NTicker>();
            foreach (var bond in bonds) {
                if (!string.IsNullOrEmpty(bond.Ticker)) 
                    SaveIdName(bond.Ticker, tickerTable, tickers);
                if (!string.IsNullOrEmpty(bond.ParentTicker)) 
                    SaveIdName(bond.ParentTicker, tickerTable, tickers);
            }
            tickerTable.Save();
            foreach (var ticker in tickers)
                tickers1[ticker.Key] = ticker.Value.id;
            
            // Tickers parent-to-child relationships
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.ParentTicker))) {
                var ticker = tickers[bond.Ticker];
                ticker.id_Parent = tickers1[bond.ParentTicker];
                tickerTable.Update(ticker); 
            }
            tickerTable.Save();

            // Seniority
            var seniorityTable = _container.ResolveCrudWithConnection<NSeniority>(_connection);
            var seniorities = seniorityTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NSeniority>();
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.Seniority))) 
                SaveIdName(bond.Seniority, seniorityTable, seniorities);
            seniorityTable.Save();
            foreach (var seniority in seniorities)
                seniorities1[seniority.Key] = seniority.Value.id;

            // Specimen
            var specimenTable = _container.ResolveCrudWithConnection<NSpecimen>(_connection);
            var specimens = specimenTable.FindAll().ToDictionary(i => i.Name, i => i); // new Dictionary<string, NSpecimen>();
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.Specimen)))
                SaveIdName(bond.Specimen, specimenTable, specimens);
            specimenTable.Save();
            foreach (var specimen in specimens)
                specimens1[specimen.Key] = specimen.Value.id;

            // Currency
            var currencyTable = _container.ResolveCrudWithConnection<NCurrency>(_connection);
            var currencies = currencyTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NCurrency>();
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.Currency)))
                SaveIdName(bond.Currency, currencyTable, currencies);
            currencyTable.Save();
            foreach (var currency in currencies)
                currencies1[currency.Key] = currency.Value.id;

            // SubIndustries
            var subIndustryTable = _container.ResolveCrudWithConnection<NSubIndustry>(_connection);
            var subIndustries = subIndustryTable.FindAll().ToDictionary(i => i.Name, i => i); // new Dictionary<string, NSubIndustry>();
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.SubIndustry)))
                SaveIdName(bond.SubIndustry, subIndustryTable, subIndustries);
            subIndustryTable.Save();
            foreach (var subIndustry in subIndustries)
                subIndustries1[subIndustry.Key] = subIndustry.Value.id;

            // Industries
            var industryTable = _container.ResolveCrudWithConnection<NIndustry>(_connection);
            var industries = industryTable.FindAll().ToDictionary(i => i.Name, i => i); //new Dictionary<string, NIndustry>();
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.Industry)))
                SaveIdName(bond.Industry, industryTable, industries);
            industryTable.Save();
            foreach (var industry in industries)
                industries1[industry.Key] = industry.Value.id;

            // Indices
            var indexTable = _container.ResolveCrudWithConnection<NIdx>(_connection);
            var indices = indexTable.FindAll().ToDictionary(i => i.Name, i => i);
            foreach (var bond in bonds.OfType<Frn>().Where(bond => !string.IsNullOrEmpty(bond.IndexName)))
                SaveIdName(bond.IndexName, indexTable, indices); // tries to add indices
            indexTable.Save();
            foreach (var index in indices)
                indices1[index.Key] = index.Value.id;
            
            // SubIndustry to Industry link
            // Tickers parent-to-child relationships
            foreach (var bond in bonds.Where(bond => !string.IsNullOrEmpty(bond.SubIndustry) && !string.IsNullOrEmpty(bond.Industry))) {
                var subIndustry = subIndustries[bond.SubIndustry];
                subIndustry.id_Industry = industries1[bond.Industry];
                subIndustryTable.Update(subIndustry);
            }
            subIndustryTable.Save(); // tries to update "where id = 0"

            // Descriptions
            var descriptionsTable = _container.ResolveCrudWithConnection<NDescription>(_connection);
            var descriptionsByRic = descriptionsTable.FindAll().ToDictionary(d => d.id_Ric, d => d);

            foreach (var bond in bonds) {
                var idRic = allRics[bond.Ric].id;
                var idIsin = allIsins.ContainsKey(bond.Isin) ? new long?(allIsins[bond.Isin].id) : null;

                var description = new NDescription {
                    RateStructure = bond.RateStructure,
                    IssueSize = bond.IssueSize,
                    Series = bond.Series,
                    Issue = bond.Issue,
                    Maturity = bond.Maturity,
                    NextCoupon = bond.NextCoupon,
                    id_Issuer = legalEntities1.GetNullable(bond.IssuerName),
                    id_Borrower = legalEntities1.GetNullable(bond.BorrowerName),
                    id_Ticker = tickers1.GetNullable(bond.Ticker),
                    id_Seniority = seniorities1.GetNullable(bond.Seniority),
                    id_SubIndustry = subIndustries1.GetNullable(bond.SubIndustry),
                    id_Specimen = specimens1.GetNullable(bond.Specimen),
                    id_Ric = idRic,
                    id_Isin = idIsin
                };
                
                if (descriptionsByRic.ContainsKey(idRic)) 
                    descriptionsTable.Update(description);
                else 
                    descriptionsTable.Create(description);

            }
            descriptionsTable.Save();
            descriptionsByRic = descriptionsTable.FindAll().ToDictionary(d => d.id_Ric, d => d); // refreshing
            
            // Instruments
            var instrumentsTable = _container.ResolveCrudWithConnection<NInstrument>(_connection);
            var instrumentsByDescr = instrumentsTable.FindAll().ToDictionary(i => i.id_Description, i => i);
            foreach (var bond in bonds) {
                var idRic = allRics[bond.Ric].id;
                var idDescr = descriptionsByRic[idRic].id;
                var instrument = new NInstrument {
                    Name = bond.ShortName,
                    id_InstrumentType = bond is Bond ? _instrumentTypes.Bond.id : _instrumentTypes.Frn.id,
                    id_Description = idDescr
                };
                if (instrumentsByDescr.ContainsKey(idDescr))
                    instrumentsTable.Update(instrument);
                else instrumentsTable.Create(instrument);
                instrumentsByDescr[idDescr] = instrument;
            }
            instrumentsTable.Save();

            // Legs
            var legsTable = _container.ResolveCrudWithConnection<NLeg>(_connection);
            var legsByInstrument = legsTable.FindAll().ToDictionary(i => i.id_Instrument, i => i);
            foreach (var instrument in bonds) {
                var idRic = allRics[instrument.Ric].id;
                var idDescr = descriptionsByRic[idRic].id;
                var idInstrument = instrumentsByDescr[idDescr].id;
                NLeg leg = null;
                if (instrument is Bond) {
                    var bond = instrument as Bond;
                    leg = new NLeg {
                        Structure = bond.BondStructure,
                        FixedRate = bond.Coupon,
                        id_Currency = currencies1.GetNullable(bond.Currency),
                        id_LegType = _legTypes.Received.id,
                        id_Instrument = idInstrument
                    };
                } else if (instrument is Frn) {
                    var note = instrument as Frn;
                    leg = new NLeg {
                        Structure = note.FrnStructure,
                        id_Index = indices1[note.IndexName],
                        Cap = note.Cap,
                        Floor = note.Floor,
                        Margin = note.Margin,
                        id_Currency = currencies1.GetNullable(note.Currency),
                        id_LegType = _legTypes.Received.id,
                        id_Instrument = idInstrument
                    };
                }

                if (leg != null) {
                    if (legsByInstrument.ContainsKey(idInstrument))
                        legsTable.Update(leg);
                    else legsTable.Create(leg);
                    legsByInstrument.Add(idInstrument, leg);
                }
            }
            legsTable.Save();

            //if (_notifications) 
            //    Notify(this, new DbEventArgs(addedInstruments, new long[] {}, new long[] {}, EventSource.Instrument));
        }

        private static void SaveLegalEntity(string name, long? idCountry, ICrud<NLegalEntity> crud, IDictionary<string, long> register, IDictionary<string, NLegalEntity> storage) {
            NLegalEntity entity;
            if (!register.ContainsKey(name)) {
                entity = new NLegalEntity {Name = name, id_Country = idCountry};
                crud.Create(entity);
            } else {
                entity = crud.FindById(register[name]);
                if (entity.Name != name || entity.id_Country != idCountry) {
                    entity.Name = name;
                    entity.id_Country = idCountry;
                    crud.Update(entity);
                }
            }
            storage[name] = entity;
        }

        private static void SaveIdName<T>(string name, ICrud<T> crud, IDictionary<string, T> storage) 
            where T : class, IIdName, IEquatable<T>, new() {
            T entity;
            if (!storage.ContainsKey(name)) {
                entity = new T {Name = name};
                crud.Create(entity);
            } else {
                entity = storage[name];
                if (entity.Name != name) {
                    entity.Name = name;
                    crud.Update(entity);
                }
            }
            storage[name] = entity;
        }

        public void SaveRatings(IEnumerable<Rating> ratings) {
            var rtis = new Dictionary<Tuple<long, long, DateTime>, NRatingToInstrument>();
            var rtcs = new Dictionary<Tuple<long, long, DateTime>, NRatingToLegalEntity>();

            if (_connection.State != ConnectionState.Open) {
                Logger.Warn(string.Format("Connection not open: {0}", _connection.State));
                _connection = _container.Resolve<IConnector>().GetConnection();
            }
            var ratingViews = _container
                .ResolveReaderWithConnection<NRatingsView>(_connection)
                .FindAll()
                .ToList();

            var instrumentRicViews = _container
                .ResolveReaderWithConnection<NInstrumentRicView>(_connection)
                .FindAll()
                .ToList();

            var rtisCrud = _container
                .ResolveCrudWithConnection<NRatingToInstrument>(_connection);

            var ratingsToInstruments = rtisCrud
                .FindAll()
                .ToList();

            var rtcsCrud = _container
                .ResolveCrudWithConnection<NRatingToLegalEntity>(_connection);
            var ratingsToLegalEntities = rtcsCrud
                .FindAll()
                .ToList();
            
            var instrumentIBViews = _container
                .ResolveReaderWithConnection<NInstrumentIBView>(_connection)
                .FindAll()
                .ToList();

            var enumerable = ratings as Rating[] ?? ratings.ToArray();

            var instruments = (
                from rating in enumerable
                where !rating.Issuer
                let r = rating
                let ratingInfo =
                    ratingViews.FirstOrDefault(x => x.AgencyCode == r.Source && x.RatingName == r.RatingName)
                where ratingInfo != null
                let ricInfo = instrumentRicViews.FirstOrDefault(x => x.Name == rating.Ric)
                where ricInfo != null
                where !ratingsToInstruments.Any(
                    x => x.RatingDate == r.Date &&
                            x.id_Instrument == ricInfo.id_Instrument &&
                            x.id_Rating == ratingInfo.id_Rating)
                select
                    new NRatingToInstrument {
                        id_Instrument = ricInfo.id_Instrument,
                        id_Rating = ratingInfo.id_Rating,
                        RatingDate = rating.Date
                    }).ToList();

            foreach (var instrument in instruments) {
                if (instrument.RatingDate == null || instrument.id_Instrument == null)
                    continue;
                var key = Tuple.Create(instrument.id_Instrument.Value, instrument.id_Rating,
                    instrument.RatingDate.Value);
                if (!rtis.ContainsKey(key))
                    rtis.Add(key, instrument);
            }

            var companies = (
                from rating in enumerable
                where rating.Issuer
                let r = rating
                let ratingInfo =
                    ratingViews.FirstOrDefault(x => x.AgencyCode == r.Source && x.RatingName == r.RatingName)
                where ratingInfo != null
                let ricInfo = instrumentIBViews.FirstOrDefault(x => x.Name == rating.Ric)
                where ricInfo != null && (ricInfo.id_Issuer.HasValue || ricInfo.id_Borrower.HasValue)
                let idLegalEntity = ricInfo.id_Issuer.HasValue ? ricInfo.id_Issuer.Value : ricInfo.id_Borrower.Value
                where !ratingsToLegalEntities.Any(
                    x => x.RatingDate == r.Date &&
                            x.id_LegalEntity == idLegalEntity &&
                            x.id_Rating == ratingInfo.id_Rating)
                select
                    new NRatingToLegalEntity {
                        id_LegalEntity = idLegalEntity,
                        id_Rating = ratingInfo.id_Rating,
                        RatingDate = rating.Date
                    }).ToList();

            foreach (var company in companies) {
                if (company.RatingDate == null || company.id_LegalEntity == null)
                    continue;
                var key = Tuple.Create(company.id_LegalEntity.Value, company.id_Rating, company.RatingDate.Value);
                if (!rtcs.ContainsKey(key))
                    rtcs.Add(key, company);
            }

            if (rtis.Any()) {
                rtis.Values.ToList().ForEach(x => rtisCrud.Create(x));
                rtisCrud.Save();
            }

            if (rtcs.Any()) {
                rtcs.Values.ToList().ForEach(x => rtcsCrud.Create(x));
                rtcsCrud.Save();
            }
        }

        private long UpdateChain(string name, long feedId, DateTime expanded, string prms) {
            var table = _container.ResolveCrudWithConnection<NChain>(_connection);
            var item = table.FindBy(t => t.Name == name).FirstOrDefault(); // todo expressions parsing

            if (item != null) {
                item.Params = prms;
                item.Expanded = expanded;
                item.id_Feed = feedId;
                table.Update(item);
            } else {
                item = new NChain { Name = name, id_Feed = feedId, Expanded = expanded, Params = prms };
                table.Create(item);
            }
            table.Save();

            return item.id;
        }

        private void AddRics(long chainId, long feedId, IEnumerable<string> rics) {
            var ricTable = _container.Resolve<ICrud<NRic>>();
            var allRics = ricTable.FindAll().ToDictionary(r => r.Name, r => r);

            var ricToChainTable = _container.Resolve<ICrud<NRicToChain>>();
            var allRicRoChains = ricToChainTable.FindAll().ToList();
            
            var theRics = rics as IList<string> ?? rics.ToList();
            foreach (var name in theRics) {
                NRic ric;
                if (allRics.ContainsKey(name)) {
                    ric = allRics[name];
                    if (ric.id_Feed != feedId) {
                        ric.id_Feed = feedId;
                        ricTable.Update(ric);
                    }
                } else {
                    ric = new NRic { Name = name, id_FieldGroup = _resolver.Resolve(name).id, id_Feed = feedId };
                    ricTable.Create(ric);
                }
                allRics[name] = ric;
            }
            ricTable.Save();

            var nRics = theRics.Select(name => allRics[name]).Where(ric => allRicRoChains.Any(rtc => rtc.id_Ric == ric.id && rtc.id_Chain != chainId));
            foreach (var ric in nRics) 
                ricToChainTable.Create(new NRicToChain { id_Chain = chainId, id_Ric = ric.id });
            
            ricToChainTable.Save();
        }

        public void SaveListRics(string listName, string[] rics, string feedName) {
            throw new NotImplementedException();
        }

        public void SaveSearchRics(string searchQuery, string[] rics, string feedName, DateTime expanded, string prms) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            _connection.Close();
            _connection.Dispose();
        }
    }
}