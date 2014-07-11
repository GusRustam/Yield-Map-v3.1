using System;
using System.Linq;
using Autofac;
using YieldMap.Database;
using YieldMap.Language;
using YieldMap.Tools.Logging;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Readers;
using YieldMap.Transitive.Domains.UnitsOfWork;
using YieldMap.Transitive.Repositories;

namespace YieldMap.Transitive.Registry {
    public class PropertiesUpdater : IPropertiesUpdater {
        private readonly IContainer _container;
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Transitive.Registry");

        public PropertiesUpdater(Func<IContainer> container) {
            _container = container.Invoke();
        }

        public int Recalculate(Func<InstrumentDescriptionView, bool> predicate = null) {
            Logger.Trace("Recalculate()");
            var reader = _container.Resolve<IInstrumentDescriptionsReader>();
            var registry = _container.Resolve<IFunctionRegistry>();
            var properties = registry.Items;
            
            if (predicate == null)
                predicate = i => true;

            using (var uow = _container.Resolve<IPropertiesUnitOfWork>()) {
                using (var propertiesRepo = _container.Resolve<IPropertyValuesRepostiory>(new NamedParameter("uow", uow))) {
                    reader.InstrumentDescriptionViews
                        .ToList()
                        .Where(predicate)
                        .Select(descr => new {
                            InstrumentId = descr.id_Instrument,
                            Environment = reader.PackInstrumentDescription(descr)
                        })
                        .ToList()
                        // for each instrument matching predicate
                        .ForEach(idDescr => {
                            var instrumentId = idDescr.InstrumentId;
                            var environment = idDescr.Environment;
                            // for each property
                            properties.ToList().ForEach(kvp => {
                                var propertyId = kvp.Key;
                                var grammar = kvp.Value;
                                // evaluate property for that instrument
                                var value = Interpreter.evaluate(grammar.Grammar, environment);

                                // is there any value for this property and instrument?
                                var item = propertiesRepo
                                    .FindBy(pv => pv.id_Instrument == instrumentId && pv.id_Property == propertyId)
                                    .FirstOrDefault();

                                if (value.IsNothing && item != null) {
                                    // there was a value, now there shouldn't be any, since new value is nothing
                                    // remove it then
                                    propertiesRepo.Remove(item);
                                } else if (!value.IsNothing) {
                                    // some value calculated
                                    var strValue = value.asString;
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
                    uow.Save();
                }
            }
                
            return 0;
        }

        public int Refresh() {
            var registry = _container.Resolve<IFunctionRegistry>();
            using (var ctx = new PropertiesContext()) 
                ctx.Properties.ToList().ForEach(p => registry.Add(p.id, p.Expression));
            return 0;
        }
    }
}
