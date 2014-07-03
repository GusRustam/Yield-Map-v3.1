using System.Data.Entity;
using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Contexts {
    public class EnumerationsContext : BaseContext<EnumerationsContext> {
        public DbSet<InstrumentType> InstrumentTypes;
        public DbSet<FieldGroup> FieldGroups;
        public DbSet<FieldDefinition> FieldDefinitions;
        public DbSet<FieldVsGroup> FieldVsGroups;
        public DbSet<LegType> LegTypes;
    }
}
