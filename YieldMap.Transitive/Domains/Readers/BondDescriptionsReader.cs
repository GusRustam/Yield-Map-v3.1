using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Readers {
    public class BondDescriptionsReader :ReadOnlyRepository<InstrumentDescriptionContext>, IBondDescriptionsReader  {
        public BondDescriptionsReader() {
        }

        public BondDescriptionsReader(InstrumentDescriptionContext context)
            : base(context) {
        }

        public IQueryable<BondDescriptionView> BondDescriptionViews {
            get { return Context.BondDescriptionViews.AsNoTracking(); }
        }


    }
}