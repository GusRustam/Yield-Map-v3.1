using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public static class LegTypes {
        public static readonly LegType Paid;
        public static readonly LegType Received;
        public static readonly LegType Both;

        static LegTypes() {
            using (var ctx = DbConn.CreateContext()) {
                Paid = ctx.LegTypes.First(i => i.Name == "Paid").ToPocoSimple();
                Received = ctx.LegTypes.First(i => i.Name == "Received").ToPocoSimple();
                Both = ctx.LegTypes.First(i => i.Name == "Both").ToPocoSimple();
            }
        }
    }
}
