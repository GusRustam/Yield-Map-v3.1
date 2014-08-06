using System;
using Autofac;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Reader;
using YieldMap.Transitive.Native.Variables;
using YieldMap.Transitive.Procedures;
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

            // Services
            Builder.Register(x => Triggers.Main).As<ITriggerManager>();
            Builder.RegisterModule<NotificationsModule>();

            Builder.RegisterType<FunctionRegistry>().As<IFunctionRegistry>().SingleInstance();
            Builder.RegisterType<NewFunctionUpdater>().As<INewFunctionUpdater>().SingleInstance();
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

            // Repos and their units of work.
            // Logic: first repos, and then - their UOWs (the UOWs they use)
            Builder.RegisterType<InstrumentRepository>().As<IInstrumentRepository>();
            Builder.RegisterType<InstrumentUnitOfWork>().As<IInstrumentUnitOfWork>();

            Builder.RegisterType<PropertiesRepository>().As<IPropertiesRepository>();
            Builder.RegisterType<PropertyValuesRepostiory>().As<IPropertyValuesRepostiory>();
            Builder.RegisterType<PropertiesUnitOfWork>().As<IPropertiesUnitOfWork>();

            // Native components
            // - connection
            Builder.RegisterType<Connector>().As<IConnector>();

            // - helpers
            Builder.RegisterType<NEntityHelper>().As<INEntityHelper>().SingleInstance();
            Builder.RegisterType<NEntityReaderHelper>().As<INEntityReaderHelper>().SingleInstance();
            Builder.RegisterType<VariableHelper>().As<IVariableHelper>().SingleInstance();
            Builder.RegisterType<NEntityCache>().As<INEntityCache>().SingleInstance();

            // - cruds
            Builder.RegisterType<ChainCrud>().As<ICrud<NChain>>();
            Builder.RegisterType<FeedCrud>().As<ICrud<NFeed>>();
            Builder.RegisterType<FieldDefinitionCrud>().As<ICrud<NFieldDefinition>>();
            Builder.RegisterType<FieldGroupCrud>().As<ICrud<NFieldGroup>>();
            Builder.RegisterType<InstrumentCrud>().As<IInstrumentCrud>();
            Builder.RegisterType<InstrumentTypeCrud>().As<ICrud<NInstrumentType>>();
            Builder.RegisterType<LegTypeCrud>().As<ICrud<NLegType>>();
            Builder.RegisterType<PropertyCrud>().As<ICrud<NProperty>>();
            Builder.RegisterType<PropertyValueCrud>().As<ICrud<NPropertyValue>>();
            Builder.RegisterType<RatingToInstrumentCrud>().As<ICrud<NRatingToInstrument>>();
            Builder.RegisterType<RatingToLegalEntityCrud>().As<ICrud<NRatingToLegalEntity>>();
            Builder.RegisterType<RicCrud>().As<ICrud<NRic>>();
            Builder.RegisterType<SourceTypeCrud>().As<ICrud<NSourceType>>();

            // - readers
            Builder.RegisterType<BondDescriptionReader>().As<IReader<NBondDescriptionView>>();
            Builder.RegisterType<FrnDescriptionReader>().As<IReader<NFrnDescriptionView>>();
            Builder.RegisterType<FieldVsGroupReader>().As<IReader<NFieldVsGroup>>();
            Builder.RegisterType<InstrumentIBViewReader>().As<IReader<NInstrumentIBView>>();
            Builder.RegisterType<InstrumentRicViewReader>().As<IReader<NInstrumentRicView>>();
            Builder.RegisterType<OrdinaryBondReader>().As<IReader<NOrdinaryBond>>();
            Builder.RegisterType<OrdinaryFrnReader>().As<IReader<NOrdinaryFrn>>();
            Builder.RegisterType<RatingViewReader>().As<IReader<NRatingView>>();
        }
    }
}
