using System;
using Autofac;
using YieldMap.Transitive.Domains.NativeContext;
using YieldMap.Transitive.Domains.Readers;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Registry;
using YieldMap.Transitive.Repositories;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive {
    public static class DatabaseBuilder {
        //private static readonly Logging.Logger Logger = Logging.LogFactory.create("YieldMap.Transitive.DatabaseBuilder");
        public static ContainerBuilder Builder { get; private set; }
        private static IContainer _container;
        private static readonly object Lock = new object();

        public static IContainer Container {
            get {
                lock (Lock) {
                    if (_container != null) return _container;
                    Builder.RegisterInstance<Func<IContainer>>(() => _container);
                    return _container = Builder.Build();
                }
            }
        }

        static DatabaseBuilder() {
            Builder = new ContainerBuilder();

            // Services
            Builder.Register(x => Triggers.Main).As<ITriggerManager>();
            Builder.RegisterModule<NotificationsModule>();

            Builder.RegisterType<FunctionRegistry>().As<IFunctionRegistry>().SingleInstance();
            Builder.RegisterType<PropertiesUpdater>().As<IPropertiesUpdater>().SingleInstance();
            // -- updates, backup/restore
            Builder.RegisterType<DbUpdates>().As<IDbUpdates>();
            Builder.RegisterType<BackupRestore>().As<IBackupRestore>();
            // -- savers
            Builder.RegisterType<Saver>().As<ISaver>();

            // Resolver
            Builder.RegisterType<FieldResolver>().As<IFieldResolver>().SingleInstance();
            
            // Enums
            Builder.RegisterType<FieldDefinitions>().As<IFieldDefinitions>().SingleInstance();
            Builder.RegisterType<FieldGroups>().As<IFieldGroups>().SingleInstance();
            Builder.RegisterType<FieldSet>().As<IFieldSet>().SingleInstance();
            Builder.RegisterType<InstrumentTypes>().As<IInstrumentTypes>().SingleInstance();
            Builder.RegisterType<LegTypes>().As<ILegTypes>().SingleInstance();

            // Readers (they provide read-only access to one or several tables in Db)
            Builder.RegisterType<FeedReader>().As<IFeedReader>();
            Builder.RegisterType<InstrumentDescriptionsReader>().As<IInstrumentDescriptionsReader>();
            Builder.RegisterType<OrdinaryFrnReader>().As<IOrdinaryFrnReader>();
            Builder.RegisterType<BondDescriptionsReader>().As<IBondDescriptionsReader>();

            // Repos and their units of work.
            // Logic: first repos, and then - their UOWs (the UOWs they use)
            Builder.RegisterType<ChainRepository>().As<IChainRepository>();
            Builder.RegisterType<RicRepository>().As<IRicRepository>();
            Builder.RegisterType<ChainRicUnitOfWork>().As<IChainRicUnitOfWork>();

            Builder.RegisterType<FeedRepository>().As<IFeedRepository>();
            Builder.RegisterType<FeedsUnitOfWork>().As<IEikonEntitiesUnitOfWork>();

            Builder.RegisterType<InstrumentRepository>().As<IInstrumentRepository>();
            Builder.RegisterType<InstrumentUnitOfWork>().As<IInstrumentUnitOfWork>();

            Builder.RegisterType<PropertiesRepository>().As<IPropertiesRepository>();
            Builder.RegisterType<PropertyValuesRepostiory>().As<IPropertyValuesRepostiory>();
            Builder.RegisterType<PropertiesUnitOfWork>().As<IPropertiesUnitOfWork>();

            // Native components
            Builder.RegisterType<Connector>().As<IConnector>();
            Builder.RegisterType<NInstrumentReader>().As<INInstrumentReader>();
            Builder.RegisterType<NPropertiesReader>().As<INPropertiesReader>();
        }
    }
}
