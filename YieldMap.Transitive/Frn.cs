using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class Frn : InstrumentDescription {
        public string FrnStructure;

        public string Description;

        public string IndexName;
        public double? Cap;
        public double? Floor;
        public double? Margin;

        protected Frn(MetaTables.BondDescr bond) : base(bond) {
        }

        public static Frn Create(MetaTables.FrnData frn, MetaTables.BondDescr bond) {
            return new Frn(bond) {
                FrnStructure = bond.BondStructure,
                Description = bond.Description,
                IndexName = frn.IndexRic,
                
                Cap = frn.Cap,
                Floor = frn.Floor,
                Margin = frn.Margin
            };
        }
    }
}
