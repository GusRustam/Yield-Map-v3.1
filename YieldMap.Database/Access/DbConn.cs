using System;
using System.Threading;
using System.Threading.Tasks;
using YieldMap.Tools.Location;

namespace YieldMap.Database.Access {
    public static class DbConn {
        private static readonly string ConnectionString;

        static DbConn () {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnectionString = MainEntities.GetConnectionString("TheMainEntities");
        }

        public static MainEntities CreateContext() {
            return new MainEntities(ConnectionString);
        }
    }
}
