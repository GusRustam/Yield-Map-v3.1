using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class ChainRics : IDisposable {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Additions.ChainRics");

        private readonly Dictionary<string, Ric> _rics = new Dictionary<string, Ric>();
        private readonly Dictionary<string, Feed> _feeds = new Dictionary<string, Feed>();
        private readonly Dictionary<string, Chain> _chains = new Dictionary<string, Chain>();
        
        public void Dispose() {
            _feeds.Clear();
            _chains.Clear();
        }

        public void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms) {
            if (prms == null) prms = string.Empty;
            using (var ctx = new MainEntities(DbConn.ConnectionString)) {
                try {
                    var feed = EnsureFeed(ctx, feedName);
                    var chain = EnsureChain(ctx, chainRic, feed, expanded, prms);

                    var existingRics = ctx.RicToChains.Where(rtc => rtc.Chain_id == chain.id).Select(rtc => rtc.Ric.Name).ToArray();
                    var newRics = new HashSet<string>(rics);
                    newRics.RemoveWhere(existingRics.Contains);

                    AddRics(ctx, chain, feed, newRics);
                    ctx.SaveChanges();
                } catch (DbEntityValidationException e) {
                    Logger.ErrorEx("Failed to save", e);
                    Logger.Report(e);
                    throw;
                }
            }
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
                    var ric = _rics.ContainsKey(name) ? _rics[name] :
                                    ctx.Rics.FirstOrDefault(r => r.Name == name) ??
                                    ctx.Rics.Add(new Ric { Name = name });

                    ric.Feed = feed;
                    _rics[name] = ric;

                    if (ric.RicToChains.All(rtc => rtc.Chain != chain))
                        ctx.RicToChains.Add(new RicToChain { Ric = ric, Chain = chain });

                } catch (DbEntityValidationException e) {
                    Logger.ErrorEx("Invalid op", e);
                } catch (DbUpdateException e) {
                    Logger.ErrorEx("Failed to update", e);
                }
            }
        }

    }
}
