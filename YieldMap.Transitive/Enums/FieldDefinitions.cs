using System;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public class FieldDefinitions : IFieldDefinitions {
        public NFieldDefinition Bid { get; private set; }
        public NFieldDefinition Ask { get; private set; }
        public NFieldDefinition Last { get; private set; }
        public NFieldDefinition Close { get; private set; }
        public NFieldDefinition Vwap { get; private set; }
        public NFieldDefinition Volume { get; private set; }
        public NFieldDefinition Value { get; private set; }
        public NFieldDefinition Tenor { get; private set; }
        public NFieldDefinition Maturity { get; private set; }

        public FieldDefinitions(Func<IContainer> containerF) {
            var items = containerF()
                .Resolve<IFieldDefinitionCrud>()
                .FindAll()
                .ToList();

            Bid = items.First(x => x.Name == "BID");
            Ask = items.First(x => x.Name == "ASK");
            Last = items.First(x => x.Name == "LAST");
            Close = items.First(x => x.Name == "CLOSE");
            Vwap = items.First(x => x.Name == "VWAP");
            Volume = items.First(x => x.Name == "VOLUME");
            Value = items.First(x => x.Name == "VALUE");
            Tenor = items.First(x => x.Name == "TENOR");
            Maturity = items.First(x => x.Name == "MATURITY");
        }
    }
}