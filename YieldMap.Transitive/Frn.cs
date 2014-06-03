using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive {
    public class Frn : InstrumentDescription {
        public string FrnStructure{ get; private set; }

        public string Description{ get; private set; }

        public string IndexName{ get; private set; }
        public double? Cap{ get; private set; }
        public double? Floor{ get; private set; }
        public double? Margin{ get; private set; }

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
