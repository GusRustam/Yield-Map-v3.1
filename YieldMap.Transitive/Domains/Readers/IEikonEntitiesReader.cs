using System.Linq;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Readers {
    public interface IEikonEntitiesReader {
        IQueryable<Feed> Feeds { get; }

        IQueryable<Chain> Chains { get; }
        IQueryable<Ric> Rics { get; }
        IQueryable<Index> Indices { get; }
        IQueryable<Isin> Isins { get; }
    }
}