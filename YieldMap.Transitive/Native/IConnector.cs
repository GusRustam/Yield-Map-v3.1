using System.Data.SQLite;

namespace YieldMap.Transitive.Native {
    public interface IConnector {
        SQLiteConnection GetConnection();
    }
}