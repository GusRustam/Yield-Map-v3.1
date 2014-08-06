using System;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public class InstrumentTypes : IInstrumentTypes {
        public NInstrumentType Bond { get; private set; }
        public NInstrumentType Frn { get; private set; }
        public NInstrumentType Swap { get; private set; }
        public NInstrumentType Ndf { get; private set; }
        public NInstrumentType Cds { get; private set; }


        public InstrumentTypes(Func<IContainer> containerF) {
            var instrumentTypes = containerF().Resolve<ICrud<NInstrumentType>>().FindAll().ToList();
            Bond = instrumentTypes.First(i => i.Name == "Bond");
            Frn = instrumentTypes.First(i => i.Name == "Frn");
            Swap = instrumentTypes.First(i => i.Name == "Swap");
            Ndf = instrumentTypes.First(i => i.Name == "Ndf");
            Cds = instrumentTypes.First(i => i.Name == "Cds");
        }
    }
}
