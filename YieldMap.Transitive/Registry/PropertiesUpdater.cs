using System;
using System.Linq;
using Autofac;
using YieldMap.Database;
using YieldMap.Transitive.Domains;
using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.Readers;

namespace YieldMap.Transitive.Registry {
    public class PropertiesUpdater : IPropertiesUpdater {
        private readonly IContainer _container;

        public PropertiesUpdater(Func<IContainer> container) {
            _container = container.Invoke();
        }

        public bool Recalculate() {
            var reader = _container.Resolve<IInstrumentDescriptionsReader>();
            var registry = _container.Resolve<IFunctionRegistry>();
            
            using (var ctx = new PropertiesContext()) {
                // storing all properties in registry
                registry.Add(ctx.Properties.Select(p => Tuple.Create(p.id, p.Expression)));
                
                var idsAndDescriptions = reader.InstrumentDescriptionViews
                    .Select(descr => new {
                        InstrumentId = descr.id_Instrument,
                        Variable = reader.PackInstrumentDescription(descr) 
                    });
                
                foreach (var idDescr in idsAndDescriptions) {
                    // a list of pairs <propertyId, value>
                    var res = registry.EvaluateAll(idDescr.Variable); 
                    foreach (var keyValue in res) {
                        var propertyId = keyValue.Item1;
                        var instrumentId = idDescr.InstrumentId;
                        var value = keyValue.Item2.asString;
                        ctx.PropertyValues.Add(new PropertyValue {
                            id_Property = propertyId,
                            id_Instrument = instrumentId,
                            Value = value,
                            State = State.Added
                        });
                    }
                }
                ctx.SaveChanges();
            }
            return true;
        }
    }
}
