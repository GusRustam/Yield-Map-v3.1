using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Enums {
    public class InstrumentTypes : IInstrumentTypes {
        public InstrumentType Bond { get; private set; }
        public InstrumentType Frn { get; private set; }
        public InstrumentType Swap { get; private set; }
        public InstrumentType Ndf { get; private set; }
        public InstrumentType Cds { get; private set; }

        public InstrumentTypes(EnumerationsContext ctx) {
            Bond = ctx.InstrumentTypes.First(i => i.Name == "Bond").ToPocoSimple();
            Frn = ctx.InstrumentTypes.First(i => i.Name == "Frn").ToPocoSimple();
            Swap = ctx.InstrumentTypes.First(i => i.Name == "Swap").ToPocoSimple();
            Ndf = ctx.InstrumentTypes.First(i => i.Name == "Ndf").ToPocoSimple();
            Cds = ctx.InstrumentTypes.First(i => i.Name == "Cds").ToPocoSimple();
        }
    }
}
