using System;
using Autofac;
using YieldMap.Transitive.Domains;
using YieldMap.Transitive.Domains.Readers;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Queries;
using YieldMap.Transitive.Registry;
using YieldMap.Transitive.Repositories;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive {
    public static class DatabaseBuilder {
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

            // --- Services
            Builder.RegisterType<FunctionRegistry>().As<IFunctionRegistry>().SingleInstance();
            Builder.RegisterType<PropertiesUpdater>().As<IPropertiesUpdater>().SingleInstance();

            // Resolver
            Builder.RegisterType<FieldResolver>().As<IFieldResolver>().SingleInstance();
            
            // Enums
            Builder.RegisterType<FieldDefinitions>().As<IFieldDefinitions>().SingleInstance();
            Builder.RegisterType<FieldGroups>().As<IFieldGroups>().SingleInstance();
            Builder.RegisterType<FieldSet>().As<IFieldSet>().SingleInstance();
            Builder.RegisterType<InstrumentTypes>().As<IInstrumentTypes>().SingleInstance();
            Builder.RegisterType<LegTypes>().As<ILegTypes>().SingleInstance();

            // Readers
            Builder.RegisterType<EikonEntitiesReader>().As<IEikonEntitiesReader>();
            Builder.RegisterType<InstrumentDescriptionsReader>().As<IInstrumentDescriptionsReader>();

            // Savers
            Builder.RegisterType<Bonds>().As<IBonds>();
            Builder.RegisterType<ChainRics>().As<IChainRics>();
            Builder.RegisterType<IPropertyStorage>().As<IPropertyStorage>(); // TODO NO PROPERTY STORER!!!
            Builder.RegisterType<Ratings>().As<IRatings>();

            // Readers and writers
            Builder.RegisterType<DbUpdates>().As<IDbUpdates>();
            Builder.RegisterType<BackupRestore>().As<IBackupRestore>();
            
            Builder.RegisterType<ChainRepository>().As<IChainRepository>();
            Builder.RegisterType<RicRepository>().As<IRicRepository>();
            Builder.RegisterType<ChainRicUnitOfWork>().As<IChainRicUnitOfWork>();

            Builder.RegisterType<EikonEntitiesUnitOfWork>().As<IEikonEntitiesUnitOfWork>();
            Builder.RegisterType<FeedRepository>().As<IFeedRepository>();

            Builder.RegisterType<BondAdditionUnitOfWork>().As<IBondAdditionUnitOfWork>();
            Builder.RegisterType<InstrumentRepository>().As<IInstrumentRepository>();

            Builder.RegisterType<PropertyValuesRepostiory>().As<IPropertyValuesRepostiory>();
            Builder.RegisterType<PropertiesUnitOfWork>().As<IPropertiesUnitOfWork>();

            Builder.RegisterType<OrdinaryFrnReader>().As<IOrdinaryFrnReader>();
            
        }

    }
}
