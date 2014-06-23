using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.StoredProcedures.Enums {
    public interface IInstrumentTypes {
        InstrumentType Bond { get; }
        InstrumentType Frn { get; }
        InstrumentType Swap { get; }
        InstrumentType Ndf { get; }
        InstrumentType Cds { get; }
    }

    public class InstrumentTypes : IInstrumentTypes {
        private readonly IDbConn _conn;

        public InstrumentType Bond { get; private set; }
        public InstrumentType Frn { get; private set; }
        public InstrumentType Swap { get; private set; }
        public InstrumentType Ndf { get; private set; }
        public InstrumentType Cds { get; private set; }

        public InstrumentTypes() {
            using (var ctx = _conn.CreateContext()) {
                Bond = ctx.InstrumentTypes.First(i => i.Name == "Bond").ToPocoSimple();
                Frn = ctx.InstrumentTypes.First(i => i.Name == "Frn").ToPocoSimple();
                Swap = ctx.InstrumentTypes.First(i => i.Name == "Swap").ToPocoSimple();
                Ndf = ctx.InstrumentTypes.First(i => i.Name == "Ndf").ToPocoSimple();
                Cds = ctx.InstrumentTypes.First(i => i.Name == "Cds").ToPocoSimple();
            }
        }

        public InstrumentTypes(IDbConn conn) {
            _conn = conn;
        }
    }
}
