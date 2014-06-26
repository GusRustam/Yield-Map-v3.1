using System;
using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.Procedures.Deletions {
    internal class Eraser : IEraser {
        private readonly MainEntities _context;

        public Eraser(IDbConn dbConn) {
            _context = dbConn.CreateContext();
        }        
        
        public void DeleteInstruments(Func<Instrument, bool> selector = null) {
            if (selector == null) selector = x => true;
            try {
                _context.Configuration.AutoDetectChangesEnabled = false;
                var bondsToDelete = _context.Instruments.ToList().Where(bond => selector(bond)).ToList();

                foreach (var bond in bondsToDelete) {
                    _context.Instruments.Remove(bond);
                }
                _context.SaveChanges();
            } finally {
                _context.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void DeleteFeeds(Func<Feed, bool> selector = null) {
            if (selector == null)
                selector = x => true;

            try {
                _context.Configuration.AutoDetectChangesEnabled = false;
                var feeds = _context.Feeds.ToList().Where(feed => selector(feed)).ToList();
                foreach (var feed in feeds) _context.Feeds.Remove(feed);
                _context.SaveChanges();
            } finally {
                _context.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void DeleteIsins(Func<Isin, bool> selector = null) {
            if (selector == null)
                selector = x => true;

            try {
                _context.Configuration.AutoDetectChangesEnabled = false;
                var isins = _context.Isins.ToList().Where(isin => selector(isin)).ToList();
                foreach (var isin in isins)
                    _context.Isins.Remove(isin);
                _context.SaveChanges();
            } finally {
                _context.Configuration.AutoDetectChangesEnabled = true;
            }            
        }

        public void DeleteChains(Func<Chain, bool> selector = null) {
            if (selector == null)
                selector = x => true;
            try {
                _context.Configuration.AutoDetectChangesEnabled = false;
                var chains = _context.Chains.ToList().Where(chain => selector(chain)).ToList();

                foreach (var chain in chains) {
                    var c = chain;
                    var links = _context.RicToChains.ToList().Where(link => link.Chain_id == c.id).ToList();
                    foreach (var link in links) _context.RicToChains.Remove(link);
                    _context.Chains.Remove(chain);
                }
                _context.SaveChanges();
            } finally {
                _context.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void DeleteRics(Func<Ric, bool> selector = null) {
            if (selector == null)
                selector = x => true;
            try {
                _context.Configuration.AutoDetectChangesEnabled = false;
                var rics = _context.Rics.ToList().Where(ric => selector(ric)).ToList();

                foreach (var ric in rics) {
                    var r = ric;
                    var links = _context.RicToChains.ToList().Where(link => link.Ric_id == r.id).ToList();
                    foreach (var link in links)
                        _context.RicToChains.Remove(link);
                    _context.Rics.Remove(ric);
                }
                _context.SaveChanges();
            } finally {
                _context.Configuration.AutoDetectChangesEnabled = true;
            }
        }
    }
}
