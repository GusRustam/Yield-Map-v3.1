using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public static class LegTypes {
        public static readonly InstrumentType Paid;
        public static readonly InstrumentType Received;
        public static readonly InstrumentType Both;

        static LegTypes() {
            using (var ctx = DbConn.CreateContext()) {
                Paid = ctx.InstrumentTypes.First(i => i.Name == "Bond").ToPocoSimple();
                Received = ctx.InstrumentTypes.First(i => i.Name == "Frn").ToPocoSimple();
                Both = ctx.InstrumentTypes.First(i => i.Name == "Swap").ToPocoSimple();
            }
        }
    }
}
