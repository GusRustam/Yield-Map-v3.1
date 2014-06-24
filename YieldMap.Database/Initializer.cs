using Autofac;
using YieldMap.Database.Access;
using YieldMap.Database.StoredProcedures;
using YieldMap.Database.StoredProcedures.Additions;
using YieldMap.Database.StoredProcedures.Deletions;
using YieldMap.Database.StoredProcedures.Enums;

namespace YieldMap.Database {
    public static class Initializer {
        public static readonly IContainer Container;

        static Initializer() {
            var builder = new ContainerBuilder();
            builder.RegisterType<DbConn>().As<IDbConn>().SingleInstance();
            builder.RegisterType<Bonds>().As<IBonds>().SingleInstance();
            builder.RegisterType<ChainRics>().As<IChainRics>().SingleInstance();
            builder.RegisterType<Ratings>().As<IRatings>().SingleInstance();
            builder.RegisterType<Eraser>().As<IEraser>().SingleInstance();
            builder.RegisterType<FieldGroups>().As<IFieldGroups>().SingleInstance();
            builder.RegisterType<InstrumentTypes>().As<IInstrumentTypes>().SingleInstance();
            builder.RegisterType<LegTypes>().As<ILegTypes>().SingleInstance();
            builder.RegisterType<BackupRestore>().As<IBackupRestore>().SingleInstance();
            builder.RegisterType<ChainsLogic>().As<IChainsLogic>().SingleInstance();
            builder.RegisterType<Refresh>().As<IRefresh>().SingleInstance();
            Container = builder.Build();
        }
    }
}
