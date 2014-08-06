using System.Reflection;

namespace YieldMap.Transitive.Native {
    public class PropertyRecord {
        private readonly PropertyInfo _info;
        private readonly string _dbName;
        private readonly string _tableName;

        public PropertyRecord(PropertyInfo info, string dbName, string tableName) {
            _info = info;
            _dbName = dbName;
            _tableName = tableName;
        }

        public string DbName {
            get { return string.IsNullOrEmpty(_dbName) ? _info.Name : _dbName; }
        }

        public PropertyInfo Info {
            get { return _info; }
        }

        public string TableName {
            get { return _tableName; }
        }
    }
}