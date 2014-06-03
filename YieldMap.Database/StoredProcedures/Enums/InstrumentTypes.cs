using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public static class InstrumentTypes {
        public static readonly InstrumentType Bond;
        public static readonly InstrumentType Frn;
        public static readonly InstrumentType Swap;
        public static readonly InstrumentType Ndf;
        public static readonly InstrumentType Cds;

        static InstrumentTypes() {
            using (var ctx = DbConn.CreateContext()) {
                Bond = ctx.InstrumentTypes.First(i => i.Name == "Bond").ToPocoSimple();
                Frn = ctx.InstrumentTypes.First(i => i.Name == "Frn").ToPocoSimple();
                Swap = ctx.InstrumentTypes.First(i => i.Name == "Swap").ToPocoSimple();
                Ndf = ctx.InstrumentTypes.First(i => i.Name == "Ndf").ToPocoSimple();
                Cds = ctx.InstrumentTypes.First(i => i.Name == "Cds").ToPocoSimple();
            }
        }
    }
}
