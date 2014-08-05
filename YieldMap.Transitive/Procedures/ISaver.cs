using System;
using System.Collections.Generic;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.MediatorTypes;
using Rating = YieldMap.Transitive.MediatorTypes.Rating;

namespace YieldMap.Transitive.Procedures {
    /// <summary>
    /// Saves instruments, ratings and also different kinds of sources (chainRics, lists and search queries)
    /// </summary>
    public interface ISaver : INotifier, IDisposable {
        void SaveInstruments(IEnumerable<InstrumentDescription> bonds);
        void SaveRatings(IEnumerable<Rating> ratings);

        void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms);
        void SaveListRics(string listName, string[] rics, string feedName);
        void SaveSearchRics(string searchQuery, string[] rics, string feedName, DateTime expanded, string prms);
    }
}