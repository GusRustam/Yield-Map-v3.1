using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class Issuer : Entity  {
        public RatingInfo Rating;
        public override Dictionary<string, object> AsVariable() {
            var res = base.AsVariable();
            res.VariableJoin(Rating.AsVariable(), "RATING");
            return res;
        }
    }
}