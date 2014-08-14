using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using YieldMap.Language;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Native;
using YieldMap.Transitive.Native.Entities;
using YieldMap.Transitive.Native.Variables;

namespace YieldMap.Transitive.Procedures {
    public class PropertyUpdater : IPropertyUpdater {
        private readonly IContainer _container;
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Transitive.Registry");

        private static readonly List<Type> InstrumentTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ITypedInstrument)))
            .ToList();

        private readonly MethodInfo _recalculate;

        public PropertyUpdater(Func<IContainer> container) {
            _container = container.Invoke();
            _recalculate = GetType().GetMethod("Recalculate");
        }

        public int Recalculate<TItem>(IEnumerable<long> ids = null) 
            where TItem : class, ITypedInstrument  {

            var reader = _container.Resolve<IReader<TItem>>();
            var registry = _container.Resolve<IFunctionRegistry>();
            var properties = registry.Items;

            var helper = _container.Resolve<IVariableHelper>();


            // all instruments matching predicate
            var list = reader
                .FindAll()
                .Where(x => ids == null || ids.Contains(x.id_Instrument))
                .Select(descr => new {
                    InstrumentId = descr.id_Instrument,
                    InstrumentTypeId = descr.id_InstrumentType,
                    Environment = helper.ToVariable(descr) 
                })
                .ToList();

            using (var crud = _container.Resolve<ICrud<NPropertyValue>>()) {
                list.ForEach(idDescr => {
                    var instrumentId = idDescr.InstrumentId;
                    Logger.Trace(string.Format("For instrument with id {0}", instrumentId));
                    var environment = idDescr.Environment;

                    // for each property
                    properties.Where(p => p.Value.IdInstrumentType == idDescr.InstrumentTypeId).ToList().ForEach(kvp => {
                        var propertyId = kvp.Key;
                        var grammar = kvp.Value;
                        Logger.Trace(string.Format("For property with id {0} grammar {1}", propertyId, grammar));
                        // evaluate property for that instrument
                        var value = Interpreter.evaluate(grammar.Grammar, grammar.Expression, environment);
                        Logger.Trace(string.Format("and value is {0}", value));

                        var item = crud
                            .FindBy(p => p.id_Instrument == instrumentId && p.id_Property == propertyId)
                            .FirstOrDefault();

                        if (value.IsNothing && item != null) {
                            // there was a value, now there shouldn't be any, since new value is nothing
                            // remove it then
                            Logger.Info("Removing");
                            crud.Delete(item);

                        } else if (!value.IsNothing) {
                            // some value calculated
                            var strValue = value.asString;
                            Logger.Info(string.Format("Adding {0}", strValue));
                            // add it
                            if (item == null) {
                                crud.Create(new NPropertyValue {
                                    id_Property = propertyId,
                                    id_Instrument = instrumentId,
                                    Value = strValue
                                });
                            } else {
                                // or update existing value
                                item.Value = strValue;
                                crud.Update(item);
                            }
                        }
                    });
                });
                return crud.Save();
            }
        }

        public int RecalculateAll(IEnumerable<long> ids = null) {
            return InstrumentTypes
                .Select(t => _recalculate.MakeGenericMethod(t))
                .Select(g => (int)g.Invoke(this, new object[] { ids }))
                .Sum();
        }
    }
}