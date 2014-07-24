using System.Data.Entity;
using YieldMap.Database;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Domains.Contexts {
    public class EnumerationsContext : BaseContext<EnumerationsContext> {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.Domains.Contexts.EnumerationsContext");
        public EnumerationsContext() {
            Logger.Debug("EnumerationsContext()");
        }  
        public DbSet<InstrumentType> InstrumentTypes { get; set; }
        public DbSet<FieldGroup> FieldGroups { get; set; }
        public DbSet<FieldDefinition> FieldDefinitions { get; set; }
        public DbSet<OrdinaryFrn> OrdinaryFrn { get; set; }
        public DbSet<OrdinaryBond> OrdinaryBond { get; set; }
        public DbSet<FieldVsGroup> FieldVsGroups { get; set; }
        public DbSet<LegType> LegTypes { get; set; }
    }
}
