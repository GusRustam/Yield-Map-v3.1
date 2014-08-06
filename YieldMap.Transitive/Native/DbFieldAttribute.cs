using System;

namespace YieldMap.Transitive.Native {
    public class DbFieldAttribute : Attribute {
        private readonly int _order;
        private readonly string _name;

        public DbFieldAttribute(int order) {
            _order = order;
        }

        public DbFieldAttribute(int order, string name) {
            _order = order;
            _name = name;
        }


        public int Order {
            get { return _order; }
        }

        public string Name {
            get { return _name; }
        }
    }
}