using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class Entity : IVariable {
        public string Name;
        public string Country;
        public virtual Dictionary<string, object> AsVariable() {
            var res = new Dictionary<string, object>();
            res["NAME"] = Name;
            res["COUNTRY"] = Country;
            return res;
        }
    }
}