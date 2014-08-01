using System.Reflection;

namespace YieldMap.Transitive.Native.Variables {
    class VariableRecord {
        private readonly PropertyInfo _info;
        private readonly string _varName;

        public VariableRecord(PropertyInfo info) {
            _info = info;
            _varName = info.GetCustomAttribute<VariableAttribute>().Name;
            if (string.IsNullOrEmpty(_varName)) _varName = _info.Name;
        }

        public PropertyInfo Info {
            get { return _info; }
        }

        public string VarName {
            get { return _varName; }
        }
    }
}