using System.Collections.Generic;
using YieldMap.Database;

namespace YieldMap.Transitive.MediatorTypes.Variables {
    public class BondView : InstrumentView {
        private readonly double? _coupon;
        public BondView(BondDescriptionView i) : base(i) {
            _coupon = i.Coupon;
        }

        public override Dictionary<string, object> Variable() {
            var res = new Dictionary<string, object>();
            res["COUPON"] = _coupon;
            res.VariableJoin(base.Variable());
            return res;
        }
    }
}