using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace YieldMap.Transitive.Native {
    public interface INEntityHelper {
        IEnumerable<string> BulkInsertSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        IEnumerable<string> BulkUpdateSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        IEnumerable<string> BulkDeleteSql<T>(IEnumerable<T> instruments) where T : class, IIdentifyable, IEquatable<T>;
        string DeleteAllSql<T>();
        string SelectSql<T>() where T : IIdentifyable;
        string FindIdSql<T>(T instrument) where T : IIdentifyable;
        T Read<T>(SQLiteDataReader reader) where T : class, IIdentifyable;
        long ReadId(SQLiteDataReader reader);
        string AllFields<T>(bool qualified = false) where T : class, IIdentifyable;
    }
}