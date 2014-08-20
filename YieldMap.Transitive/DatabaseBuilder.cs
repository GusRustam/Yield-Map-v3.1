using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        static Assembly GenerateAssembly<T>(Type baseInterface, Type abstractBase, Type helperType, AssemblyName assemblyName, string suffix) {
            // Define the assembly and the module.
            var appDomain = AppDomain.CurrentDomain;
            var assembly = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            var module = assembly.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

            /////////////////////////////////////////////////////////////////////
            // Find entities which need cruds 
            // 

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();
            var identifyables = FindNotImplemented<T>(baseInterface, allTypes);

            foreach (var identifyable in identifyables) {
                // Declare the class "ClassA"
                var genericCrudBase = abstractBase.MakeGenericType(new[] { identifyable });

                var baseConstructor1 = genericCrudBase.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new[] { typeof(Func<IContainer>) }, null);
                Debug.Assert(baseConstructor1 != null, "baseConstructor1 != null");
                var baseConstructor2 = genericCrudBase.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new[] { typeof(SQLiteConnection), helperType }, null);
                Debug.Assert(baseConstructor2 != null, "baseConstructor2 != null");

                var crudName = identifyable.Name + suffix;
                TheLogger.Info(string.Format("Creating {0}", crudName));

                // Creating class
                var crudClass = module.DefineType(crudName, TypeAttributes.Public, genericCrudBase);

                // Adding static field
                var theLoggerField = crudClass.DefineField("TheLogger", typeof (Logging.Logger),
                    FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly);
                
                // Creating static constructor
                var initializer = crudClass.DefineTypeInitializer();
                var ilinit = initializer.GetILGenerator();

                ilinit.Emit(OpCodes.Ldstr, assemblyName.Name + "." + crudName);
                ilinit.Emit(OpCodes.Call, typeof(Logging.LogFactory).GetMethod("create", new []{typeof(string)}));
                ilinit.Emit(OpCodes.Stsfld, theLoggerField);
                ilinit.Emit(OpCodes.Ret);

                // Creating inherited property
                var loggerProperty = crudClass.DefineProperty("Logger", PropertyAttributes.HasDefault, CallingConventions.Any, typeof (Logging.Logger), Type.EmptyTypes);
                
                const MethodAttributes getterAttrs = 
                    MethodAttributes.Virtual | 
                    MethodAttributes.HideBySig |
                    MethodAttributes.Public | 
                    MethodAttributes.SpecialName;

                // Creating reader for that property
                var loggerPropertyReader = crudClass.DefineMethod("get_Logger", getterAttrs, typeof(Logging.Logger), Type.EmptyTypes);
                var ilprg = loggerPropertyReader.GetILGenerator();

                ilprg.Emit(OpCodes.Ldsfld, theLoggerField);
                ilprg.Emit(OpCodes.Ret);

                // Setting reader
                loggerProperty.SetGetMethod(loggerPropertyReader);

                // Creating constructors
                var constructor1 = crudClass.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard, new[] {typeof (Func<IContainer>)});

                // ReSharper disable once InconsistentNaming
                var ilc1g = constructor1.GetILGenerator();
                ilc1g.Emit(OpCodes.Ldarg_0);
                ilc1g.Emit(OpCodes.Ldarg_1);
                ilc1g.Emit(OpCodes.Call, baseConstructor1);
                ilc1g.Emit(OpCodes.Ret);

                var constructor2 = crudClass.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard, new[] { typeof(SQLiteConnection), helperType });

                // ReSharper disable once InconsistentNaming
                var ilc2g = constructor2.GetILGenerator();
                ilc2g.Emit(OpCodes.Ldarg_0);
                ilc2g.Emit(OpCodes.Ldarg_1);
                ilc2g.Emit(OpCodes.Ldarg_2);
                ilc2g.Emit(OpCodes.Call, baseConstructor2);
                ilc2g.Emit(OpCodes.Ret);

                crudClass.CreateType();
            }

            assembly.Save(assemblyName.Name + ".dll"); // todo do I need to save it?
            return assembly;
        }

        private static IEnumerable<Type> FindNotImplemented<T>(Type baseGeneric, Type[] allTypes) {
            return allTypes
                .Select(t => {
                    var interfaces = t.GetInterfaces();

                    if (t.IsClass && !t.IsAbstract && interfaces.Any(x => x == typeof(T))) {
                        TheLogger.Trace(string.Format("Hope to keep {0}", t.Name));

                        var itsGenericBase = baseGeneric.MakeGenericType(new[] { t });

                        // Checking if this generic interface is already implemented
                        var implementers = allTypes
                            .Select(type => new {
                                Type = type,
                                Interface = type.GetInterfaces().FirstOrDefault(x => x == itsGenericBase)
                            })
                            .Where(x => x.Interface != null)
                            .ToList();

                        if (!implementers.Any()) {
                            TheLogger.Info(string.Format("Keeping {0}: not implemented elsewhere", t.Name));
                            return t;
                        }
                        var names = string.Join(",", implementers.Select(i => i.Type.Name));
                        TheLogger.Warn(string.Format("Skipping {0} it is already implemented in {1}", t.Name, names));
                    }
                    TheLogger.Trace(string.Format("Skipping {0}", t.Name));
                    return null;
                })
                .Where(x => x != null)
                .ToList();
        }

        static DatabaseBuilder() {
            var allTypes = new Set<Type>(Assembly.GetExecutingAssembly().GetTypes());
            
            var crudTypes = new Set<Type>(GenerateAssembly<IIdentifyable>(
                    typeof(ICrud<>), 
                    typeof(CrudBase<>), 
                    typeof(INEntityHelper), 
                    new AssemblyName("YieldMap.Cruds"),
                    "Crud")
                .GetTypes()) + allTypes;

            var readerTypes = new Set<Type>(GenerateAssembly<INotIdentifyable>(
                    typeof(IReader<>), 
                    typeof(ReaderBase<>), 
                    typeof(INEntityReaderHelper), 
                    new AssemblyName("YieldMap.Readers"),
                    "Reader")
                .GetTypes()) + allTypes; 

            Builder = new ContainerBuilder();
            
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
            var cruds = crudTypes
                 .Select(t => {
                     var genericInterfaces = t.GetInterfaces().Where(x => x.IsGenericType).ToList();

                     if (t.IsClass && !t.IsAbstract &&
                         genericInterfaces.Any(x => x.GetGenericTypeDefinition() == typeof (ICrud<>))) {
                         TheLogger.Trace(string.Format("Keeping {0}", t.Name));
                         return new {
                             Type = t,
                             Interface = genericInterfaces.First(x => x.GetGenericTypeDefinition() == typeof (ICrud<>))
                         };
                     }
                     TheLogger.Trace(string.Format("Skipping {0}", t.Name));
                     return null;
                 })
                 .Where(x => x != null)
                 .ToList();

            cruds.ForEach(t => {
                TheLogger.Info(string.Format("Registering ICrud<{0}>", t.Type.Name));
                Builder.RegisterType(t.Type).As(t.Interface)
                    .UsingConstructor(typeof(Func<IContainer>));
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof (Func<IContainer>))
                    .Keyed(ConnectionMode.New, t.Interface);
                Builder.RegisterType(t.Type)
                    .UsingConstructor(typeof (SQLiteConnection), typeof(INEntityHelper))
                    .Keyed(ConnectionMode.Existing, t.Interface);
            });

            // - readers
            var readers = readerTypes
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
                Builder.RegisterType(t.Type).As(t.Interface)
                    .UsingConstructor(typeof(Func<IContainer>));
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
