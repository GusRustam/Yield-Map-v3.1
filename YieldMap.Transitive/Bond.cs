using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class Bond : InstrumentDescription {
        public string BondStructure;
        public double? Coupon;

        protected Bond(MetaTables.BondDescr bond) : base(bond) {
        }

        public static Bond Create(MetaTables.BondDescr bond) {
            return new Bond(bond) {
                BondStructure = bond.BondStructure,
                Coupon = bond.Coupon
            };
        }
    }
}