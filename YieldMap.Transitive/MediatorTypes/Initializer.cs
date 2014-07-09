using Autofac;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Queries;

namespace YieldMap.Transitive.MediatorTypes {
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
