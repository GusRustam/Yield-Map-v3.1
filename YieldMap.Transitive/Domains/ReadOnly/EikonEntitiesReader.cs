using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Queries;

namespace YieldMap.Transitive.Domains.ReadOnly {
    public class EikonEntitiesReader : ReadOnlyRepository<EikonEntitiesContext>, IEikonEntitiesReader {
        public EikonEntitiesReader() {}
        public EikonEntitiesReader(EikonEntitiesContext context) : base(context) {}
        
        public IQueryable<Feed> Feeds {
            get {
                return Context.Feeds.AsNoTracking();
            }
        }
        public IQueryable<Chain> Chains {
            get {
                return Context.Chains.AsNoTracking();
            }
        }
        public IQueryable<Ric> Rics {
            get {
                return Context.Rics.AsNoTracking();
            }
        }
        public IQueryable<Index> Indices {
            get {
                return Context.Indices.AsNoTracking();
            }
        }
        public IQueryable<Isin> Isins {
            get {
                return Context.Isins.AsNoTracking();
            }
        }
    }
}