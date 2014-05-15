using System;
using System.Linq;

namespace YieldMap.Database {
    public class Eraser : IDisposable {
        private readonly MainEntities _ctx = new MainEntities(DbConn.ConnectionString);

        public void DeleteBonds(Func<InstrumentBond, bool> selector = null) {
            if (selector == null) selector = x => true;
            try {
                _ctx.Configuration.AutoDetectChangesEnabled = false;
                var bondsToDelete = _ctx.InstrumentBonds.ToList().Where(bond => selector(bond)).ToList();

                foreach (var bond in bondsToDelete) {
                    _ctx.InstrumentBonds.Remove(bond);
                }
                _ctx.SaveChanges();
            } finally {
                _ctx.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void DeleteFeeds(Func<Feed, bool> selector = null) {
            if (selector == null)
                selector = x => true;

            try {
                _ctx.Configuration.AutoDetectChangesEnabled = false;
                var feeds = _ctx.Feeds.ToList().Where(feed => selector(feed)).ToList();
                foreach (var feed in feeds)  _ctx.Feeds.Remove(feed);
                _ctx.SaveChanges();
            } finally {
                _ctx.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void DeleteChains(Func<Chain, bool> selector = null) {
            if (selector == null)
                selector = x => true;
            try {
                _ctx.Configuration.AutoDetectChangesEnabled = false;
                var chains = _ctx.Chains.ToList().Where(chain => selector(chain)).ToList();

                foreach (var chain in chains) {
                    var c = chain;
                    var links = _ctx.RicToChains.ToList().Where(link => link.Chain_id == c.id).ToList();
                    foreach (var link in links) _ctx.RicToChains.Remove(link);
                    _ctx.Chains.Remove(chain);
                }
                _ctx.SaveChanges();
            } finally {
                _ctx.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void Dispose() {
            _ctx.Dispose();
        }
    }
}
