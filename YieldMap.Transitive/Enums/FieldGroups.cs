using System;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    internal class FieldGroups : IFieldGroups {
        public NFieldGroup Default { get; private set; }
        public NFieldGroup Micex { get; private set; }
        public NFieldGroup Eurobonds { get; private set; }
        public NFieldGroup RussiaCpi { get; private set; }
        public NFieldGroup Mosprime { get; private set; }
        public NFieldGroup Swaps { get; private set; }

        public FieldGroups(Func<IContainer> containerF) {
            var reader = containerF().Resolve<IFieldGroupCrud>();

            var items = reader.FindAll().ToList();
            Default = items.First(x => x.Default);
            Micex = items.First(x => x.Name == "Micex");
            Eurobonds = items.First(x => x.Name == "Eurobonds");
            RussiaCpi = items.First(x => x.Name == "Russian CPI Index");
            Mosprime = items.First(x => x.Name == "Mosprime");
            Swaps = items.First(x => x.Name == "Swaps");
        }
    }
}
