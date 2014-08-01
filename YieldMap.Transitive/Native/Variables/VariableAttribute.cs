using System;

namespace YieldMap.Transitive.Native.Variables {
    public class VariableAttribute : Attribute {
        private readonly string _name;

        public VariableAttribute(string name) {
            _name = name;
        }
        public VariableAttribute() {
            _name = String.Empty;
        }

        public string Name {
            get { return _name; }
        }
    }
}