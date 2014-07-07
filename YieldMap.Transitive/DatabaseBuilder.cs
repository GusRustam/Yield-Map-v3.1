using Autofac;
using YieldMap.Transitive.Domains.Enums;
using YieldMap.Transitive.Domains.Procedures;
using YieldMap.Transitive.Domains.Queries;
using YieldMap.Transitive.Domains.ReadOnly;
using YieldMap.Transitive.Domains.Repositories;
using YieldMap.Transitive.Registry;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive {
    public static class DatabaseBuilder {
        public static ContainerBuilder Builder { get; private set; }

        static DatabaseBuilder() {
            Builder = new ContainerBuilder();

            // --- Services
            Builder.RegisterType<IFunctionRegistry>().As<FunctionRegistry>().SingleInstance();
            Builder.RegisterType<IPropertiesUpdater>().As<PropertiesUpdater>().SingleInstance();

            // Resolver
            Builder.RegisterType<IFieldResolver>().As<FieldResolver>().SingleInstance();
            
            // Enums
            Builder.RegisterType<IFieldDefinitions>().As<FieldDefinitions>().SingleInstance();
            Builder.RegisterType<IFieldGroups>().As<FieldGroups>().SingleInstance();
            Builder.RegisterType<IFieldSet>().As<FieldSet>().SingleInstance();
            Builder.RegisterType<IInstrumentTypes>().As<InstrumentTypes>().SingleInstance();
            Builder.RegisterType<ILegTypes>().As<LegTypes>().SingleInstance();

            // Readers
            Builder.RegisterType<IEikonEntitiesReader>().As<EikonEntitiesReader>();
            Builder.RegisterType<IInstrumentDescriptionsReader>().As<InstrumentDescriptionsReader>();

            // Savers
            Builder.RegisterType<IBonds>().As<Bonds>();
            Builder.RegisterType<IChainRics>().As<ChainRics>();
            Builder.RegisterType<IPropertyStorage>().As<IPropertyStorage>(); // TODO NO PROPERTY STORER!!!
            Builder.RegisterType<IRatings>().As<Ratings>();

            // Readers and writers
            Builder.RegisterType<IBackupRestore>().As<BackupRestore>();
            Builder.RegisterType<IChainRicUnitOfWork>().As<ChainRicUnitOfWork>();

            Builder.RegisterType<IDbUpdates>().As<DbUpdates>();
            Builder.RegisterType<IFeedRepository>().As<FeedRepository>();
        }
    }
}
