using System.Collections.Generic;
using System.Linq;

namespace YieldMap.Transitive.Events {
    public class ManyTables : IManyTables {
        private IEnumerable<ISingleTable> _tables;

        public ManyTables(IEnumerable<ISingleTable> tables) {
            Tables = tables;
        }

        public IEnumerable<ISingleTable> Tables {
            get { return new List<ISingleTable>(_tables); }
            private set { _tables = value; }
        }

        public override string ToString() {
            return "Updates of many tables:\n" + string.Join(",\n", _tables.Select(t => t.ToString()));
        }
    }
}