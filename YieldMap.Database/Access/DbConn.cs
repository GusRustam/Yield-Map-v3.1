using YieldMap.Tools.Location;

namespace YieldMap.Database.Access {
    internal class DbConn : IDbConn {
        private readonly string _connectionString;

        public DbConn () {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            _connectionString = MainEntities.GetConnectionString("TheMainEntities");
        }

        public MainEntities CreateContext() {
            return new MainEntities(_connectionString);
        }
    }
}
