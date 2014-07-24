using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class PropertiesContext : BaseContext<PropertiesContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.PropertiesContext");
        public PropertiesContext() {
            Logger.Debug("PropertiesContext()");
        }  
        
        
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyValue> PropertyValues { get; set; }
    }
}
