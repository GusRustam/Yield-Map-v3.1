using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;

namespace YieldMap.Transitive.Enums {
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


        private static string GetById(IEnumerable<NFieldVsGroup> items, long id) {
            var item = items.FirstOrDefault(x => x.id_FieldDefinition == id);
            return item != null ? item.InternalName : String.Empty;
        }

        public FieldSet(Func<IContainer> containerF, long idFieldGroup) {
            var container = containerF();

            var defs = container.Resolve<IFieldDefinitions>();
            var fvg = container.Resolve<IReader<NFieldVsGroup>>();

            var items = fvg.FindBy(x => x.id_FieldGroup == idFieldGroup).ToList();
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