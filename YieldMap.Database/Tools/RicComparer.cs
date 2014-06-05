using System.Collections.Generic;

namespace YieldMap.Database.Tools {
    public class RicComparer : IEqualityComparer<string> {
        public bool Equals(string x, string y) {
            var theX = new RicString(x);
            var theY = new RicString(y);
            return theX.Equals(theY);
        }

        public int GetHashCode(string obj) {
            return new RicString(obj).GetHashCode();
        }
    }
}