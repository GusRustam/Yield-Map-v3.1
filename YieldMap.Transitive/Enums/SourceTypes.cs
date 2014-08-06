using System;
using System.Linq;
using Autofac;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public class SourceTypes : ISourceTypes {
        public NSourceType Universe { get; private set; }
        public NSourceType Chain { get; private set; }
        public NSourceType List { get; private set; }
        public NSourceType Query { get; private set; }
        public NSourceType Source { get; private set; }

        public SourceTypes(Func<IContainer> containerF) {
            using (var crud = containerF.Invoke().Resolve<ICrud<NSourceType>>()) {
                var items = crud.FindAll().ToList();
                Universe = items.First(x => x.Name == "Universe");
                Chain = items.First(x => x.Name == "Chain");
                List = items.First(x => x.Name == "List");
                Query = items.First(x => x.Name == "Query");
                Source = items.First(x => x.Name == "Source");
            }

        }
    }
}