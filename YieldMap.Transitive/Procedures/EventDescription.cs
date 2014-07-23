using System.Collections.Generic;
using System.Linq;

namespace YieldMap.Transitive.Procedures {
    public class EventDescription : IEventDescription {
        private readonly IEnumerable<long[]> _ids;

        public EventDescription(IEnumerable<long[]> ids) {
            _ids = ids;
        }

        public override string ToString() {
            var v = (_ids.Any() ? _ids.SelectMany(x => x) : new long[]{}).ToList();
            return v.Any() ? string.Join(", ", v) : "None";
        }

        public IEnumerable<long[]> Ids {
            get { return _ids; }
        }
    }
}