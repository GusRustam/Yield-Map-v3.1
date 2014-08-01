using System.Data.SQLite;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public interface INEntityReaderHelper {
        string SelectSql<T>();
        T Read<T>(SQLiteDataReader reader) where T : class, INotIdentifyable;
    }
}