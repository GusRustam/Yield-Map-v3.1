using System;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    internal class LegTypes : ILegTypes {
        public NLegType Paid { get; private set; }
        public NLegType Received { get; private set; }
        public NLegType Both { get; private set; }

        public LegTypes(Func<IContainer> containerF) {
            var legTypes = containerF().Resolve<ILegTypeCrud>().FindAll().ToList();
            Paid = legTypes.First(i => i.Name == "Paid");
            Received = legTypes.First(i => i.Name == "Received");
            Both = legTypes.First(i => i.Name == "Both");
        }
    }
}
