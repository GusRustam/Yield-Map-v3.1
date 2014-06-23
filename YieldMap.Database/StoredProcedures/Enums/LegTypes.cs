using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public interface ILegTypes {
        LegType Paid { get; }
        LegType Received { get; }
        LegType Both { get; }
    }

    internal class LegTypes : ILegTypes {
        public LegType Paid { get; private set; }
        public LegType Received { get; private set; }
        public LegType Both { get; private set; }

        public LegTypes(IDbConn conn) {
            using (var ctx = conn.CreateContext()) {
                Paid = ctx.LegTypes.First(i => i.Name == "Paid").ToPocoSimple();
                Received = ctx.LegTypes.First(i => i.Name == "Received").ToPocoSimple();
                Both = ctx.LegTypes.First(i => i.Name == "Both").ToPocoSimple();
            }
        }
    }
}
