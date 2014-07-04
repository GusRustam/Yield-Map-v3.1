using System;
using System.Linq;

using Autofac;

using YieldMap.Transitive.Domains.Contexts;
using YieldMap.Transitive.Domains.ReadOnly;

namespace YieldMap.Transitive.Domains.Procedures {
    public interface IPropertiesUpdater {
    }

    //IInstrumentDescriptionsReader

    //type IPropertyStorage = abstract Save : _ -> unit

    //type Recounter = abstract member Recount : unit -> unit
    //type DbRecounter(cont : Func<IContainer>) = 
    //    let container = cont.Invoke ()
    //    let registry = container.Resolve<Registry> ()
    //    let reader = container.Resolve<IInstrumentDescriptionsReader> ()
    //    let propertySaver = container.Resolve<IPropertyStorage> ()

    //    interface Recounter with
    //        member x.Recount () = 
    //            reader.Instruments 
    //            |> Seq.choose (fun i -> 
    //                let descr = query { for d in reader.InstrumentDescriptionViews do
    //                                    where (d.id_Instrument = i.id)                                         
    //                                    select (reader.PackInstrumentDescription d) 
    //                                    exactlyOneOrDefault }                
    //                if descr <> null
    //                then Some (i.id, registry.EvaluateAll descr |> Map.ofSeq)
    //                else None)
    //            |> Map.ofSeq
    //            |> Map.toDict2 // (idInstrument (idProperty, value) Dict) Dict
    //            |> propertySaver.Save 
    
    public class PropertiesUpdater : IPropertiesUpdater {
        private readonly IContainer _container;

        public PropertiesUpdater(IContainer container) {
            _container = container;
        }

        public void Recalculate() {
            var reader = _container.Resolve<IInstrumentDescriptionsReader>();
            
            using (var ctx = new PropertiesContext()) {
                var ids = reader.Instruments
                    .Select(i => new {
                        Instrument = i, 
                        Descr = reader.InstrumentDescriptionViews.FirstOrDefault(d => d.id_Instrument == i.id),
                    })
                    .Where(d => d.Descr!= null)
                    .Select(d => new {
                        InstrumentId = d.Instrument.id, 
                        Pack = reader.PackInstrumentDescription(d.Descr)
                    });
                var ids1 = ids;
                var p1 = ctx.Properties
                    .Where(p => !String.IsNullOrWhiteSpace(p.Expression))
                    .SelectMany(p => ids1.Select(i => i));
                ids = ids;
            }
        }
    }
}
