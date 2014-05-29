using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Database.Access;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class Ratings : AccessToDb, IDisposable {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Additions.Ratings");


        public void SaveIssueRatings(IEnumerable<MetaTables.IssueRatingData> bonds) {
        }

        public void SaveIssuerRatings(IEnumerable<MetaTables.IssuerRatingData> bonds) {
        }

        public void Dispose() {
        }
    }
}
