using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database;
using YieldMap.Transitive.Domains.Contexts;

namespace YieldMap.Transitive.Domains.Enums {
    public class FieldSet : IFieldSet {
        public string Bid { get; private set; }
        public string Ask { get; private set; }
        public string Last { get; private set; }
        public string Close { get; private set; }
        public string Vwap { get; private set; }
        public string Volume { get; private set; }
        public string Value { get; private set; }
        public string Tenor { get; private set; }
        public string Maturity { get; private set; }

        private static string GetById(IEnumerable<FieldVsGroup> items, long id) {
            var item = items.FirstOrDefault(x => x.id_FieldDefinition == id);
            return item != null ? item.InternalName : String.Empty;
        }

        public FieldSet(IFieldDefinitions defs, long idFieldGroup) {
            var ctx = new EnumerationsContext();

            var items = ctx.FieldVsGroups.Where(x => x.id_FieldGroup == idFieldGroup);
            Bid = GetById(items, defs.Bid.id);
            Ask = GetById(items, defs.Ask.id);
            Last = GetById(items, defs.Last.id);
            Close = GetById(items, defs.Close.id);
            Vwap = GetById(items, defs.Vwap.id);
            Volume = GetById(items, defs.Volume.id);
            Value = GetById(items, defs.Value.id);
            Tenor = GetById(items, defs.Tenor.id);
            Maturity = GetById(items, defs.Maturity.id);
        }
    }
}