using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    /// 
    /// 
    /// 
    /// 
    public static class Additions {
        private static readonly string ConnStr;
        static Additions() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms) {
            using (var ctx = new MainEntities(ConnStr)) {
                var feed = ctx.EnsureFeed(feedName);
                var chain = ctx.EnsureChain(chainRic, feed, expanded, prms);

                var existingRics = new HashSet<string>(from r in chain.Rics select r.Name);
                var newRics = new HashSet<string>(rics);
                newRics.RemoveWhere(existingRics.Contains);

                ctx.AddRics(chain, feed, newRics);
            }
        }

        private static void AddRics(this MainEntities ctx, Chain chain, Feed feed, IEnumerable<string> rics) {
            foreach (var name in rics) {
                try {
                    var ric = ctx.Rics.FirstOrDefault(r => r.Name == name && r.Feed.id == feed.id);
                    if (ric == null) {
                        ric = ctx.Rics.Add(new Ric {Name = name, Feed = feed});
                        ctx.SaveChanges();
                    }

                    if (ric.Chains.Contains(chain))
                        continue;

                    ric.Chains.Add(chain);
                    ctx.SaveChanges();
                } catch (DbEntityValidationException e) {
                    e = e;
                }
            }
        }

        public static void DeleteBonds(HashSet<string> names) {
            using (var ctx = new MainEntities(ConnStr)) {
                var rics = from ric in ctx.Rics
                           where names.Contains(ric.Name)
                           select ric;

                // todo DELETING CAREFULLY!!!

            }
        }

        public static void SaveFrns(IEnumerable<MetaTables.FrnData> bonds) {
        }

        public static void SaveIssueRatings(IEnumerable<MetaTables.IssueRatingData> bonds) {
        }

        public static void SaveIssuerRatings(IEnumerable<MetaTables.IssuerRatingData> bonds) {
        }

        public static IEnumerable<Tuple<MetaTables.BondDescr, Exception>> SaveBonds(IEnumerable<MetaTables.BondDescr> bonds) {
            var res = new List<Tuple<MetaTables.BondDescr, Exception>>();
            var theBonds = bonds as IList<MetaTables.BondDescr> ?? bonds.ToList();

            var bondsToSave = new Dictionary<string, InstrumentBond>();
            using (var ctx = new MainEntities(ConnStr)) {
                foreach (var bond in theBonds) {
                    InstrumentBond instrument = null;
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
                            NextCoupon = bond.NextCoupon
                        };

                        // Handling issuer, borrower and their countries
                        var issuerCountry = ctx.EnsureCountry(bond.IssuerCountry);
                        var borrowerCountry = ctx.EnsureCountry(bond.BorrowerCountry);

                        instrument.Issuer = ctx.EnsureIssuer(bond.IssuerName, issuerCountry);
                        instrument.Borrower = ctx.EnsureBorrower(bond.BorrowerName, borrowerCountry);
                        instrument.Currency = ctx.EnsureCurrency(bond.Currency);
                        instrument.Ticker = ctx.EnsureTicker(bond.Ticker, bond.ParentTicker);
                        instrument.Seniority = ctx.EnsureSeniority(bond.Seniority);
                        instrument.SubIndustry = ctx.EnsureIndustry(bond.Industry, bond.SubIndustry);

                        instrument.Ric = ctx.Rics.First(r => r.Name == bond.Ric); // there already must be some ric with some feed!!!
                        
                        var isin = ctx.EnsureIsin(bond.Isin, instrument.Ric);
                        instrument.Isin = isin;

                        bondsToSave[bond.Ric] = instrument;
                    } catch (Exception e) {
                        res.Add(Tuple.Create(bond, e));
                    }
                    if (instrument != null)
                        ctx.InstrumentBonds.Add(instrument);
                }
                ctx.SaveChanges();
            }
            return res;
        }

        private static Feed EnsureFeed(this MainEntities ctx, string name) {
            var feed = ctx.Feeds.FirstOrDefault(t => t.Name == name);
            if (feed != null)
                return feed;

            feed = ctx.Feeds.Add(new Feed { Name = name });
            ctx.SaveChanges();
            return feed;
        }

        private static Chain EnsureChain(this MainEntities ctx, string name, Feed feed, DateTime expanded, string prms) {
            var chain = ctx.Chains.FirstOrDefault(t => t.Name == name);
            if (chain != null) {
                if (chain.Expanded != expanded) {
                    chain.Expanded = expanded;
                    ctx.SaveChanges();
                }
            } else {
                chain = ctx.Chains.Add(new Chain { Name = name, Feed = feed, Expanded = expanded, Params = prms });
                ctx.SaveChanges();
            }
            return chain;
        }

        private static Isin EnsureIsin(this MainEntities ctx, string name, Ric ric) {
            var isin = ctx.Isins.FirstOrDefault(i => i.Name == name && i.Feed == ric.Feed);
            if (isin == null) {
                isin = ctx.Isins.Add(new Isin { Name = name, Feed = ric.Feed });
                ctx.SaveChanges();
            }
            return isin;
        }

        private static SubIndustry EnsureIndustry(this MainEntities ctx, string ind, string sub) {
            var subIndustry = ctx.SubIndustries.FirstOrDefault(t => t.Name == sub);
            if (subIndustry != null) return subIndustry;

            var industry = ctx.Industries.FirstOrDefault(t => t.Name == ind) ??
                           ctx.Industries.Add(new Industry {Name = ind});

            subIndustry = ctx.SubIndustries.Add(new SubIndustry {Name = sub, Industry = industry});
            ctx.SaveChanges();

            return subIndustry;
        }

        private static Seniority EnsureSeniority(this MainEntities ctx, string name) {
            var pt = ctx.Seniorities.FirstOrDefault(t => t.Name == name);
            if (pt != null) return pt;

            pt = ctx.Seniorities.Add(new Seniority {Name = name});
            ctx.SaveChanges();
            return pt;
        }

        private static Ticker EnsureTicker(this MainEntities ctx, string child, string parent) {
            Ticker ch;
            if (String.IsNullOrEmpty(parent)) {
                ch = ctx.Tickers.FirstOrDefault(t => t.Name == child && t.Parent == null);
                if (ch != null) return ch;

                ch = ctx.Tickers.Add(new Ticker {Name = child});
            } else {
                var pt = ctx.EnsureParentTicker(parent);
                ch = ctx.Tickers.FirstOrDefault(t => t.Name == child && t.Parent != null && t.Parent.Name == parent);
                if (ch != null) return ch;

                ch = ctx.Tickers.Add(new Ticker {Name = child, Parent = pt});
            }
            ctx.SaveChanges();
            return ch;
        }

        private static Ticker EnsureParentTicker(this MainEntities ctx, string name) {
            var pt = ctx.Tickers.FirstOrDefault(t => t.Name == name);
            if (pt != null) return pt;
                
            pt = ctx.Tickers.Add(new Ticker { Name = name });
            ctx.SaveChanges();
            return pt;
        }

        private static Currency EnsureCurrency(this MainEntities ctx, string name) {
            var currency = ctx.Currencies.FirstOrDefault(b => b.Name == name);
            if (currency != null) return currency;

            currency = ctx.Currencies.Add(new Currency {Name = name});
            ctx.SaveChanges();
            return currency;
        }

        private static Borrower EnsureBorrower(this MainEntities ctx, string name, Country country) {
            var borrower = ctx.Borrowers.FirstOrDefault(b => b.Name == name);
            if (borrower != null) return borrower;

            borrower = ctx.Borrowers.Add(new Borrower {Name = name, Country = country});
            ctx.SaveChanges();
            return borrower;
        }

        private static Issuer EnsureIssuer(this MainEntities ctx, string name, Country country) {
            var issuer = ctx.Issuers.FirstOrDefault(b => b.Name == name);
            if (issuer != null) return issuer;

            issuer = ctx.Issuers.Add(new Issuer {Name = name, Country = country});
            ctx.SaveChanges();
            return issuer;
        }

        private static Country EnsureCountry(this MainEntities ctx, string name) {
            var country = ctx.Countries.FirstOrDefault(c => c.Name == name);
            if (country != null) return country;

            country = ctx.Countries.Add(new Country {Name = name});
            ctx.SaveChanges();
            return country;
        }

    }
}