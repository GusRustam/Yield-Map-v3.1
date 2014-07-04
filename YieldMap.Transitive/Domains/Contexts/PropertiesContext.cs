using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class PropertiesContext : BaseContext<PropertiesContext> {
        public DbSet<Property> Properties;
    }
}
