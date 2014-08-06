using System.Collections.Generic;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
     public interface IInstrumentCrud : ICrud<NInstrument> {
         IEnumerable<NInstrument> FindByRic(IEnumerable<string> rics);
     }
}
