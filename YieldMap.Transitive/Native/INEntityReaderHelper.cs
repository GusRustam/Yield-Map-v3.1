using System.Data.SQLite;

namespace YieldMap.Transitive.Native {
    public interface INEntityReaderHelper {
        string SelectSql<T>();
        T Read<T>(SQLiteDataReader reader) where T : class, INotIdentifyable;
    }
}