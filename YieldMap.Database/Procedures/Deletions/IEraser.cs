using System;

namespace YieldMap.Database.Procedures.Deletions {
    public interface IEraser {
        void DeleteInstruments(Func<Instrument, bool> selector = null);
        void DeleteFeeds(Func<Feed, bool> selector = null);
        void DeleteIsins(Func<Isin, bool> selector = null);
        void DeleteChains(Func<Chain, bool> selector = null);
        void DeleteRics(Func<Ric, bool> selector = null);
    }
}