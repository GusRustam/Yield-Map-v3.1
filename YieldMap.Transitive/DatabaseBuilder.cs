using System;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using Autofac;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Variables;
using YieldMap.Transitive.Procedures;
using YieldMap.Transitive.Tools;

namespace YieldMap.Transitive {
    public static class DatabaseBuilder {
        public static ContainerBuilder Builder { get; private set; }
        private static IContainer _container;
        private static readonly object Lock = new object();
        private static readonly Logging.Logger TheLogger = Logging.LogFactory.create("YieldMap.Transitive.DatabaseBuilder");

        public static IContainer Container {
            get {
                lock (Lock) {
                    if (_container != null) return _container; 
                    Builder.RegisterInstance<Func<IContainer>>(() => _container);
                    try {
                        _container =  Builder.Build();
                    } catch (Exception e) {
                        TheLogger.ErrorEx("Failed to build container", e);
                        throw;
                    }
                    return _container;
                }
            }
        }

        static DatabaseBuilder() {
            Builder = new ContainerBuilder();

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            // Services
            Builder.Register(x => Triggers.Main).As<ITriggerManager>();
            Builder.RegisterModule<NotificationsModule>();

            Builder.RegisterType<FunctionRegistry>().As<IFunctionRegistry>().SingleInstance();
            Builder.RegisterType<PropertyUpdater>().As<IPropertyUpdater>().SingleInstance();
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

            // Native components
            // - connection
            Builder.RegisterType<Connector>().As<IConnector>();

            // - helpers
            Builder.RegisterType<NEntityHelper>().As<INEntityHelper>().SingleInstance();
            Builder.RegisterType<NEntityReaderHelper>().As<INEntityReaderHelper>().SingleInstance();
            Builder.RegisterType<VariableHelper>().As<IVariableHelper>().SingleInstance();
            Builder.RegisterType<NEntityCache>().As<INEntityCache>().SingleInstance();

            // - cruds
            var cruds = allTypes
                 .Select(t => {
                     var genericInterfaces = t.GetInterfaces().Where(x => x.IsGenericType).ToList();

                     if (t.IsClass && !t.IsAbstract && genericInterfaces.Any(x => x.GetGenericTypeDefinition() == typeof(ICrud<>))) {
                         return new {
                             Type = t,
                             Interface = genericInterfaces.First(x => x.GetGenericTypeDefinition() == typeof(ICrud<>))
                         };
                     }
                     return null;
                 })
                 .Where(x => x != null)
                 .ToList();

            cruds.ForEach(t => {
                Builder.RegisterType(t.Type).As(t.Interface);
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof (Func<IContainer>))
                    .Keyed(ConnectionMode.New, t.Interface);
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof (SQLiteConnection), typeof(INEntityHelper))
                    .Keyed(ConnectionMode.Existing, t.Interface);
            });

            // - readers
            var readers = allTypes
                 .Select(t => {
                     var genericInterfaces = t.GetInterfaces().Where(x => x.IsGenericType).ToList();

                     if (t.IsClass && !t.IsAbstract && genericInterfaces.Any(x => x.GetGenericTypeDefinition() == typeof(IReader<>))) {
                         return new {
                             Type = t,
                             Interface = genericInterfaces.First(x => x.GetGenericTypeDefinition() == typeof(IReader<>))
                         };
                     }
                     return null;
                 })
                 .Where(x => x != null)
                 .ToList();

            readers.ForEach(t => {
                Builder.RegisterType(t.Type).As(t.Interface);
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof(Func<IContainer>))
                    .Keyed(ConnectionMode.New, t.Interface);
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof(SQLiteConnection), typeof(INEntityReaderHelper))
                    .Keyed(ConnectionMode.Existing, t.Interface);
            });
        }
    }
}
