using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Location;

namespace YieldMap.Database.StoredProcedures {
    static class Extensions {
        public static bool InRange(this DateTime pivot, DateTime what, int range) {
            return what - pivot <= TimeSpan.FromDays(range);
        }

        public static bool NeedsRefresh(this Chain chain, DateTime today) {
            return chain.Expanded.HasValue && chain.Expanded.Value < today;
        }

        public static bool NeedsRefresh(this InstrumentBond bond, DateTime today) {
            return bond.LastOptionDate.HasValue && bond.LastOptionDate.Value.InRange(today, 7);  //todo why 7?
        }

        public static HashSet<T> Add<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Add(item));
            return res;
        }

        public static HashSet<T> Remove<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Remove(item));
            return res;
        }
    }

    public enum Mission {
        Obsolete,
        ToReload,
        Keep
    }

    /// Some considerations:
    /// 
    /// BondRics = All rics from Table InstrumentBond
    ///
    /// BondRics =
    /// |--> Obsolete (those who have no bond, or those who have matured)
    /// |--> ToReload (those who need reloading)
    /// |--> Keep     (others)
    /// 
    /// ChainRics
    /// |--> New       |--> ToReload
    /// |--> Existing  |--> ToReload
    ///                |--> Keep
    /// 
    /// 
    public static class ChainsLogic {
        public static Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var existing = new HashSet<Ric>();
            var obsolete = new HashSet<Ric>();
            var toReload = new HashSet<Ric>();

            existing = existing.Add(Refresh.AllBondRics());
            obsolete = obsolete.Add(Refresh.ObsoleteBondRics(dt));  // obsolete done!
            toReload = toReload.Add(Refresh.StaleBondRics(dt));     // new rics to be added!
            
            var keep = existing.Remove(toReload).Remove(obsolete);

            var existingNames = new HashSet<string>(existing.Select(ric => ric.Name));
            var toRleoadNames = new HashSet<string>(toReload.Select(ric => ric.Name));

            var newRics = existingNames.Remove(chainRics);
            toRleoadNames = toRleoadNames.Add(newRics);

            res[Mission.Obsolete] = obsolete.Select(ric => ric.Name).ToArray();
            res[Mission.Keep] = keep.Select(ric => ric.Name).ToArray();
            res[Mission.ToReload] = toRleoadNames.ToArray();

            return res;
        }
    }

    public static class Additions {
        private static readonly string ConnStr;
        static Additions() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static void SaveBonds(IEnumerable<MetaTables.BondDescr> bonds) {
            var theBonds = bonds as IList<MetaTables.BondDescr> ?? bonds.ToList();

            var bondsToSave = new Dictionary<string, InstrumentBond>();
            using (var ctx = new MainEntities(ConnStr)) {
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
                            Maturity = bond.Maturity
                        };

                        var issuerCountry = EnsureCountry(bond.IssuerCountry);
                        var borrowerCountry = EnsureCountry(bond.BorrowerCountry);

                        var issuer = EnsureIssuer(bond.IssuerName, issuerCountry);
                        var borrower = EnsureBorrower(bond.BorrowerName, borrowerCountry);

                        instrument.Issuer = issuer;
                        instrument.Borrower = borrower;

                        bondsToSave[bond.Ric] = instrument;
                        // todo: NextOptionDate, LastOptionDate
                        // todo: Ticker, ParentTicker, Ric, Seniority, Industry, SubIndustry

                    } catch (Exception e) {
                        failed = true;
                    }
                    if (!failed) ctx.InstrumentBonds.Add(instrument);
                }
            }
        }

        private static Borrower EnsureBorrower(string name, Country country) {
            using (var ctx = new MainEntities(ConnStr)) {
                if (!ctx.Borrowers.Any(b => b.Name == name))
                    ctx.Borrowers.Add(new Borrower { Name = name, Country = country});
                ctx.SaveChanges();
                return ctx.Borrowers.First(b => b.Name == name);
            }
        }

        private static Issuer EnsureIssuer(string name, Country country) {
            using (var ctx = new MainEntities(ConnStr)) {
                if (!ctx.Issuers.Any(b => b.Name == name))
                    ctx.Issuers.Add(new Issuer { Name = name, Country = country });
                ctx.SaveChanges();
                return ctx.Issuers.First(b => b.Name == name);
            }
        }

        private static Country EnsureCountry(string name) {
            using (var ctx = new MainEntities(ConnStr)) {
                if (!ctx.Countries.Any(country => country.Name == name)) 
                    ctx.Countries.Add(new Country {Name = name});
                ctx.SaveChanges();
                return ctx.Countries.First(country => country.Name == name);
            }
        }
    }

    public static class Refresh {
        private static readonly string ConnStr;
        static Refresh() {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnStr = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static IEnumerable<Chain> ChainsInNeed(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from c in ctx.Chains where c.NeedsRefresh(dt) select c;
            }
        }

        /// <summary>
        /// Enumerates rics of all EXISTING bonds that need to be refreshed
        /// Condition for refresh is presense of embedded option which is due in +/- 7 days from today
        /// irrespective to if these rics come from chains or they are standalone
        /// </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static IEnumerable<Ric> StaleBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds where b.NeedsRefresh(dt) select b.Ric;
            }
        }

        public static IEnumerable<Ric> AllBondRics() {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds select b.Ric;
            }
        }

        /// <summary> Enumerates rics which belong to matured bonds </summary>
        /// <param name="dt">Today's date</param>
        /// <returns>IEnumerable of Rics</returns>
        public static IEnumerable<Ric> ObsoleteBondRics(DateTime dt) {
            using (var ctx = new MainEntities(ConnStr)) {
                return from b in ctx.InstrumentBonds 
                       where b.Maturity.HasValue && b.Maturity.Value < dt 
                       select b.Ric;
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
    }
}