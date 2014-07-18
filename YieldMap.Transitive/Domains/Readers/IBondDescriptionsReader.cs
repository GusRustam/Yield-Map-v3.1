using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Readers {
    public interface IBondDescriptionsReader {
        IQueryable<BondDescriptionView> BondDescriptionViews { get; }
    }
}