using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Transitive.MediatorTypes;
using Rating = YieldMap.Transitive.MediatorTypes.Rating;

namespace YieldMap.Transitive.Procedures {
    public interface ISaver {
        void SaveInstruments(IEnumerable<InstrumentDescription> bonds);
        void SaveRatings(IEnumerable<Rating> ratings);

        void SaveChainRics(string chainRic, string[] rics, string feedName, DateTime expanded, string prms);
        void SaveListRics(string listName, string[] rics, string feedName);
        void SaveSearchRics(string searchQuery, string[] rics, string feedName, DateTime expanded, string prms);
    }
}