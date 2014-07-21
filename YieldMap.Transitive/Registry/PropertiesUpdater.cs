using System;
using System.Linq;
using Autofac;
using YieldMap.Database;
using YieldMap.Language;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Readers;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.MediatorTypes.Variables;
using YieldMap.Transitive.Repositories;

namespace YieldMap.Transitive.Registry {
    public class PropertiesUpdater : IPropertiesUpdater {
        private readonly IContainer _container;
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Transitive.Registry");

        public PropertiesUpdater(Func<IContainer> container) {
            _container = container.Invoke();
        }

        public int RecalculateBonds(Func<BondDescriptionView, bool> predicate = null) {
            Logger.Trace("RecalculateBonds()");
            var reader = _container.Resolve<IBondDescriptionsReader>();
            var registry = _container.Resolve<IFunctionRegistry>();
            var properties = registry.Items;

            var instrumentTypes = _container.Resolve<IInstrumentTypes>();
            
            if (predicate == null)
                predicate = i => true;

            using (var uow = _container.Resolve<IPropertiesUnitOfWork>()) {
                using (var propertiesRepo = _container.Resolve<IPropertyValuesRepostiory>(new NamedParameter("uow", uow))) {
                    // for each instrument matching predicate
                    reader
                        .BondDescriptionViews
                        .Where(p => p.id_InstrumentType == instrumentTypes.Bond.id)
                        .ToList()
                        .Where(predicate)
                        .Select(descr => new {
                            TypeId = descr.id_InstrumentType,
                            InstrumentId = descr.id_Instrument,
                            Environment = new BondView(descr) // todo this could be generalized via factory with multimethod
                        })
                        .ToList()
                        .ForEach(idDescr => {
                            Logger.Trace(string.Format("For instrument with id {0}", idDescr.InstrumentId));
                            var instrumentId = idDescr.InstrumentId;
                            var typeId = idDescr.TypeId;
                            var environment = idDescr.Environment;
                            // for each property
                            properties.ToList().ForEach(kvp => {
                                var propertyId = kvp.Key;
                                var grammar = kvp.Value;
                                Logger.Trace(string.Format("For property with id {0} grammar {1}", propertyId, grammar));
                                // evaluate property for that instrument
                                var value = Interpreter.evaluate(grammar.Grammar, grammar.Expression, environment.AsVariable());
                                Logger.Trace(string.Format("and value is {0}", value));

                                // is there any value for this property and instrument?
                                var item = propertiesRepo
                                    .FindBy(pv => pv.id_Instrument == instrumentId && pv.id_Property == propertyId && pv.id_Instrument == typeId)
                                    .FirstOrDefault();

                                if (value.IsNothing && item != null) {
                                    // there was a value, now there shouldn't be any, since new value is nothing
                                    // remove it then
                                    Logger.Info("Removing");
                                    propertiesRepo.Remove(item);
                                } else if (!value.IsNothing) {
                                    // some value calculated
                                    var strValue = value.asString;
                                    Logger.Info(string.Format("Adding {0}", strValue));
                                    // add it
                                    if (item == null)
                                        propertiesRepo.Add(new PropertyValue {
                                            id_Property = propertyId,
                                            id_Instrument = instrumentId,
                                            Value = strValue
                                        });
                                    // or update existing value
                                    else item.Value = strValue;
                                }
                            });
                        });
                    return uow.Save();
                }
            }
        }

        public int Refresh() {
            var registry = _container.Resolve<IFunctionRegistry>();
            using (var ctx = new PropertiesContext()) 
                ctx.Properties.ToList().ForEach(p => registry.Add(p.id, p.Expression));
            return 0;
        }
    }
}
