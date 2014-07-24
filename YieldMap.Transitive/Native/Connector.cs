using System.Data.SQLite;
using System.IO;
using YieldMap.Tools.Location;

namespace YieldMap.Transitive.Native {
    public class Connector : IConnector {
        public SQLiteConnection GetConnection() {
            var connectionString = "Data Source="+Path.Combine(Location.path, "main.db")+";Version=3;New=False;Compress=True;";
            return new SQLiteConnection(connectionString);
        }
    }
}