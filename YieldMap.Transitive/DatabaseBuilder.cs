using System;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
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
                    if (_container != null) return _container; // TODO WHY TWICE??
                    Builder.RegisterInstance<Func<IContainer>>(() => _container);
                    _container = Builder.Build();
                    return _container;
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
            var allTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes();

            var crudTypes = allTypes
                 .Where(t => t.IsClass && 
                     !t.IsAbstract && 
                     t.GetInterfaces()
                        .Where(x => x.IsGenericType)
                        .Select(x => x.GetGenericTypeDefinition())
                        .Contains(typeof(ICrud<>)))
                 .ToList();

            crudTypes.ForEach(t => Builder.RegisterType(t).AsImplementedInterfaces());
            crudTypes.ForEach(t => Builder.RegisterType(t).UsingConstructor(typeof(Func<IContainer>)).Keyed(ConnectionMode.New, typeof(ICrud<>)));
            crudTypes.ForEach(t => Builder.RegisterType(t).UsingConstructor(typeof(SQLiteConnection)).Keyed(ConnectionMode.Existing, t));

            //Builder.RegisterType<ChainCrud>().As<ICrud<NChain>>();
            //Builder.RegisterType<FeedCrud>().As<ICrud<NFeed>>();
            //Builder.RegisterType<FieldDefinitionCrud>().As<ICrud<NFieldDefinition>>();
            //Builder.RegisterType<FieldGroupCrud>().As<ICrud<NFieldGroup>>();
            //Builder.RegisterType<InstrumentCrud>().As<IInstrumentCrud>();
            //Builder.RegisterType<InstrumentTypeCrud>().As<ICrud<NInstrumentType>>();
            //Builder.RegisterType<LegTypeCrud>().As<ICrud<NLegType>>();
            //Builder.RegisterType<PropertyCrud>().As<ICrud<NProperty>>();
            //Builder.RegisterType<PropertyValueCrud>().As<ICrud<NPropertyValue>>();
            //Builder.RegisterType<RatingToInstrumentCrud>().As<ICrud<NRatingToInstrument>>();
            //Builder.RegisterType<RatingToLegalEntityCrud>().As<ICrud<NRatingToLegalEntity>>();
            //Builder.RegisterType<RicCrud>().As<ICrud<NRic>>();

            //Builder.RegisterType<SourceTypeCrud>().As<ICrud<NSourceType>>();
            //Builder.RegisterType<SourceTypeCrud>().UsingConstructor(typeof(Func<IContainer>)).Keyed<ICrud<NSourceType>>(ConnectionMode.New);
            //Builder.RegisterType<SourceTypeCrud>().UsingConstructor(typeof(SQLiteConnection)).Keyed<ICrud<NSourceType>>(ConnectionMode.Existing);


            //Builder.RegisterType<IndexCrud>().As<ICrud<NIdx>>();

            // - readers
            Builder.RegisterType<BondDescriptionReader>().As<IReader<NBondDescriptionView>>();
            Builder.RegisterType<FrnDescriptionReader>().As<IReader<NFrnDescriptionView>>();
            Builder.RegisterType<FieldVsGroupReader>().As<IReader<NFieldVsGroup>>();
            Builder.RegisterType<InstrumentIBViewReader>().As<IReader<NInstrumentIBView>>();
            Builder.RegisterType<InstrumentRicViewReader>().As<IReader<NInstrumentRicView>>();
            Builder.RegisterType<OrdinaryBondReader>().As<IReader<NOrdinaryBond>>();
            Builder.RegisterType<OrdinaryFrnReader>().As<IReader<NOrdinaryFrn>>();
            
            Builder.RegisterType<RatingViewReader>().As<IReader<NRatingView>>();
            Builder.RegisterType<RatingViewReader>().UsingConstructor(typeof(Func<IContainer>)).Keyed<IReader<NRatingView>>(ConnectionMode.New);
            Builder.RegisterType<RatingViewReader>().UsingConstructor(typeof(SQLiteConnection)).Keyed<IReader<NRatingView>>(ConnectionMode.Existing);
        }
    }
}
