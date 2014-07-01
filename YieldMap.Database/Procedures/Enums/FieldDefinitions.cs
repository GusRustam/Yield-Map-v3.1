using System.Linq;
using YieldMap.Database.Access;

namespace YieldMap.Database.Procedures.Enums {
    public class FieldDefinitions : IFieldDefinitions {
        public FieldDefinition Bid { get; private set; }
        public FieldDefinition Ask { get; private set; }
        public FieldDefinition Last { get; private set; }
        public FieldDefinition Close { get; private set; }
        public FieldDefinition Vwap { get; private set; }
        public FieldDefinition Volume { get; private set; }
        public FieldDefinition Value { get; private set; }
        public FieldDefinition Tenor { get; private set; }
        public FieldDefinition Maturity { get; private set; }

        public FieldDefinitions(IDbConn conn) {
            using (var ctx = conn.CreateContext()) {
                var items = ctx.FieldDefinitions.ToList();
                Bid = items.First(x => x.Name == "BID").ToPocoSimple();
                Ask = items.First(x => x.Name == "ASK").ToPocoSimple();
                Last = items.First(x => x.Name == "LAST").ToPocoSimple();
                Close = items.First(x => x.Name == "CLOSE").ToPocoSimple();
                Volume = items.First(x => x.Name == "VOLUME").ToPocoSimple();
                Value = items.First(x => x.Name == "VALUE").ToPocoSimple();
                Tenor = items.First(x => x.Name == "TENOR").ToPocoSimple();
                Maturity = items.First(x => x.Name == "MATURITY").ToPocoSimple();
            }
        }
    }
}