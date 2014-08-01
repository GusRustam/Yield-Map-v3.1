using System.Collections.Generic;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class Issuer : Entity  {
        public RatingInfo Rating;
        public override Dictionary<string, object> Variable() {
            var res = base.Variable();
            res.VariableJoin(Rating.Variable(), "RATING");
            return res;
        }
    }
}