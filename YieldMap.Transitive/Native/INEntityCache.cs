using System;
using System.Collections.Generic;
using System.Data.SQLite;
using YieldMap.Transitive.Native.Crud;
using YieldMap.Transitive.Native.Reader;

namespace YieldMap.Transitive.Native {
    public interface INEntityCache {
        Dictionary<Type, Dictionary<Operations, string>> Queries { get; }
        Dictionary<Type, PropertyRecord[]> Properties { get; }
        Dictionary<Type, Func<SQLiteDataReader, object>> Readers { get; }
        Dictionary<Type, Dictionary<string, Func<object, string>>> Rules { get; }
        void PrepareProperties(Type type);
        void PrepareReaders(Type type);
        void PrepareRules(Type type);
    }
}