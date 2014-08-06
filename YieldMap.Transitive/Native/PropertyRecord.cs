using System.Reflection;

namespace YieldMap.Transitive.Native {
    public class PropertyRecord {
        private readonly PropertyInfo _info;
        private readonly string _dbName;

        public PropertyRecord(PropertyInfo info, string dbName) {
            _info = info;
            _dbName = dbName;
        }

        public string DbName {
            get { return string.IsNullOrEmpty(_dbName) ? _info.Name : _dbName; }
        }

        public PropertyInfo Info {
            get { return _info; }
        }
    }
}