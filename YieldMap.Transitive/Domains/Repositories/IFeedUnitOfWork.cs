using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Repositories {
    public interface IFeedUnitOfWork : IUnitOfWork<EikonEntitiesContext> {
    }
}
