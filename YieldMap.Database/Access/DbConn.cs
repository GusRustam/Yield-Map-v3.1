using YieldMap.Database.StoredProcedures.Additions;
using YieldMap.Tools.Location;

namespace YieldMap.Database.Access {
    public class DbConn : IDbConn {
        private readonly string _connectionString;

        internal static readonly DbConn Instance = new DbConn();

        private DbConn () {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            _connectionString = MainEntities.GetConnectionString("TheMainEntities");
        }

        public MainEntities CreateContext() {
            return new MainEntities(_connectionString);
        }

        public Bonds CreateBonds() {
            return new Bonds(this);
        }

        public ChainRics CreateRics() {
            return new ChainRics(this);
        }

        public Ratings CreateRatings() {
            return new Ratings(this);
        }
    }
}
