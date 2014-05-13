using YieldMap.Tools.Location;

namespace YieldMap.Database {
    public static class DbConn {
        public static string ConnectionString;
        static DbConn () {
            MainEntities.SetVariable("PathToTheDatabase", Location.path);
            ConnectionString = MainEntities.GetConnectionString("TheMainEntities");
        }
    }
}
