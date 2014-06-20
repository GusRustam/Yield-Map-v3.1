using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public class FieldGroups {
        public static readonly FieldGroup Default;
        public static readonly FieldGroup Micex;
        public static readonly FieldGroup Eurobonds;

        static FieldGroups() {
            using (var ctx = DbConn.Instance.CreateContext()) {
                var items = ctx.FieldGroups.ToList();
                Default = items.First(x => x.Default).ToPocoSimple();
                Micex = items.First(x => x.Name == "Micex").ToPocoSimple();
                Eurobonds = items.First(x => x.Name == "Russian CPI Index").ToPocoSimple();
            }
        }
    }
}
