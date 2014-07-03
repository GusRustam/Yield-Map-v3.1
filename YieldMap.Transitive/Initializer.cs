using Autofac;
using YieldMap.Transitive.Domains.Enums;
using YieldMap.Transitive.Domains.Procedures;
using YieldMap.Transitive.Domains.Queries;

namespace YieldMap.Transitive {
    public static class Initializer {
        public static readonly IContainer Container;

        static Initializer() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Bonds>().As<IBonds>().SingleInstance();
            builder.RegisterType<ChainRics>().As<IChainRics>().SingleInstance();
            builder.RegisterType<Ratings>().As<IRatings>().SingleInstance();
            builder.RegisterType<FieldGroups>().As<IFieldGroups>().SingleInstance();
            builder.RegisterType<InstrumentTypes>().As<IInstrumentTypes>().SingleInstance();
            builder.RegisterType<LegTypes>().As<ILegTypes>().SingleInstance();
            builder.RegisterType<BackupRestore>().As<IBackupRestore>().SingleInstance();
            Container = builder.Build();
        }
    }
}
