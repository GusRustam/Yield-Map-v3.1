using System;

namespace YieldMap.Transitive.Native.Entities {
    public class DbFieldAttribute : Attribute {
        private readonly int _order;

        public DbFieldAttribute(int order) {
            _order = order;
        }

        public int Order {
            get { return _order; }
        }
    }
}