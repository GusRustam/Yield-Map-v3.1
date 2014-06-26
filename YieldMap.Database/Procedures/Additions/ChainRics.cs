using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using YieldMap.Database.Access;
using YieldMap.Database.Procedures.Enums;
using YieldMap.Database.Tools;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.Procedures.Additions {
    internal class ChainRics : IChainRics {
        private readonly IFieldGroups _fieldGroups;
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Database.Additions.ChainRics");
        private readonly MainEntities _context;

        public ChainRics(IDbConn dbConn, IFieldGroups fieldGroups) {
            _fieldGroups = fieldGroups;
            _context = dbConn.CreateContext();
        }

        private readonly Dictionary<string, Ric> _rics = new Dictionary<string, Ric>();
        private readonly Dictionary<string, Feed> _feeds = new Dictionary<string, Feed>();
        private readonly Dictionary<string, Chain> _chains = new Dictionary<string, Chain>();

        public void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms) {
            if (prms == null) prms = string.Empty;

            try {
                var feed = EnsureFeed(_context, feedName);
                var chain = EnsureChain(_context, chainRic, feed, expanded, prms);

                var existingRics = _context.RicToChains.Where(rtc => rtc.Chain_id == chain.id).Select(rtc => rtc.Ric.Name).ToArray();
                var newRics = new HashSet<string>(rics);
                newRics.RemoveWhere(existingRics.Contains);

                AddRics(_context, chain, feed, newRics);
                _context.SaveChanges();
            } catch (DbEntityValidationException e) {
                Logger.Report("Failed to save", e);
                throw;
            }
        }

        public void DeleteRics(HashSet<string> rics) {
            // todo
        }

        private Feed EnsureFeed(MainEntities ctx, string name) {
            if (_feeds.ContainsKey(name)) 
                return _feeds[name];

            var feed = ctx.Feeds.FirstOrDefault(t => t.Name == name) ??
                       ctx.Feeds.Add(new Feed { Name = name });
            
            _feeds[name] = feed;
            return feed;
        }

        private Chain EnsureChain(MainEntities ctx, string name, Feed feed, DateTime expanded, string prms) {
            var chain = _chains.ContainsKey(name) ? _chains[name] : ctx.Chains.FirstOrDefault(t => t.Name == name);

            if (chain != null) {
                chain.Params = prms;
                chain.Expanded = expanded;
                chain.Feed = feed;
            } else {
                chain = ctx.Chains.Add(new Chain { Name = name, Feed = feed, Expanded = expanded, Params = prms });
            }

            _chains[name] = chain;
            return chain;
        }

        private void AddRics(MainEntities ctx, Chain chain, Feed feed, IEnumerable<string> rics) {
            foreach (var name in rics) {
                try {
                    ctx.Configuration.AutoDetectChangesEnabled = false;
                    var ric = _rics.ContainsKey(name)
                        ? _rics[name]
                        : ctx.Rics.FirstOrDefault(r => r.Name == name) ??
                          ctx.Rics.Add(new Ric {Name = name, id_FieldGroup = ResolveFieldGroup(name).id});

                    ric.Feed = feed;
                    _rics[name] = ric;

                    if (ric.RicToChains.All(rtc => rtc.Chain != chain))
                        ctx.RicToChains.Add(new RicToChain {Ric = ric, Chain = chain});

                } catch (DbEntityValidationException e) {
                    Logger.ErrorEx("Invalid op", e);
                } catch (DbUpdateException e) {
                    Logger.ErrorEx("Failed to update", e);
                } finally {
                    ctx.Configuration.AutoDetectChangesEnabled = true;
                }
            }
        }

        private FieldGroup ResolveFieldGroup(string ric) {
            if (ric.Contains("=MM")) 
                return _fieldGroups.Micex;

            return _fieldGroups.Default;
        }
    }
}
