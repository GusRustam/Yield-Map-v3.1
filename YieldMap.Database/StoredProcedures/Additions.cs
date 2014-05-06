using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    public static class Additions {
        private static readonly string ConnStr;
        static Additions() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static void SaveChainRics(string chainRic, string[] rics, string feed, DateTime expanded, string prms) {
            using (var ctx = new MainEntities(ConnStr)) {
                var theFeed = ctx.EnsureFeed(feed);
                var chain = ctx.EnsureChain(chainRic, theFeed, expanded, prms);
            }
        }

        private static Feed EnsureFeed(this MainEntities ctx, string name) {
            var feed = ctx.Feeds.FirstOrDefault(t => t.Name == name);
            if (feed != null) return feed;

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
                chain = ctx.Chains.Add(new Chain {Name = name, Feed = feed, Expanded = expanded, Params = prms});
                ctx.SaveChanges();
            }
            return chain;
        }

        public static void SaveBonds(IEnumerable<MetaTables.BondDescr> bonds) {
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

                        instrument.Ric = ctx.EnsureRic(bond.Ric, bond.Isin);
                        

                        bondsToSave[bond.Ric] = instrument;
                        // todo: Ric

                    } catch (Exception) {
                        // todo some reporting on errors
                    }
                    if (instrument != null)
                        ctx.InstrumentBonds.Add(instrument);
                }
            }
        }

        private static Ric EnsureRic(this MainEntities ctx, string ric, string isin) {
            return null; // todo
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

        public static void DeleteBonds(HashSet<string> names) {
            using (var ctx = new MainEntities(ConnStr)) {
                var rics = from ric in ctx.Rics
                           where names.Contains(ric.Name)
                           select ric;

                // todo DELETING CAREFULLY!!!

            }
        }

    }
}