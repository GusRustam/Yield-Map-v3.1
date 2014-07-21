using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class RatingInfo : IVariable {
        public string Name;
        public string Agency;
        public DateTime? Date;
        public Dictionary<string, object> AsVariable() {
            var res = new Dictionary<string, object>();
            res["NAME"] = Name;
            res["AGENCY"] = Agency;
            res["DATE"] = Agency;
            return res;
        }
    }
}