using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Enums {
    internal class LegTypes : ILegTypes {
        public LegType Paid { get; private set; }
        public LegType Received { get; private set; }
        public LegType Both { get; private set; }

        public LegTypes(EnumerationsContext ctx) {
            Paid = ctx.LegTypes.First(i => i.Name == "Paid").ToPocoSimple();
            Received = ctx.LegTypes.First(i => i.Name == "Received").ToPocoSimple();
            Both = ctx.LegTypes.First(i => i.Name == "Both").ToPocoSimple();
        }
    }
}
