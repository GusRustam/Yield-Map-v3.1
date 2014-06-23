using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    internal class FieldGroups : IFieldGroups {
        public FieldGroup Default { get; private set; }
        public FieldGroup Micex { get; private set; }
        public FieldGroup Eurobonds { get; private set; }

        public FieldGroups(IDbConn conn) {
            using (var ctx = conn.CreateContext()) {
                var items = ctx.FieldGroups.ToList();
                Default = items.First(x => x.Default).ToPocoSimple();
                Micex = items.First(x => x.Name == "Micex").ToPocoSimple();
                Eurobonds = items.First(x => x.Name == "Russian CPI Index").ToPocoSimple();
            }
        }
    }

    public interface IFieldGroups {
        FieldGroup Default { get; }
        FieldGroup Micex { get; }
        FieldGroup Eurobonds { get; }
    }
}
