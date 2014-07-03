using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.Tools {
    public class RicString : IComparable<RicString>, IEqualityComparer<RicString> {
        private readonly string _ric;
        private readonly string _undelayed;
        private readonly bool _isDelayed;
        
        public RicString(string ric) {
            _ric = ric;
            if (string.IsNullOrWhiteSpace(ric)) throw new ArgumentException("ric null or whitespace");
            _isDelayed = _ric[0] == '/';
            _undelayed = _isDelayed ? _ric.Substring(1) : _ric;
        }

        protected bool Equals(RicString other) {
            return string.Equals(Undelayed, other.Undelayed);
        }

        public int CompareTo(RicString other) {
            return String.CompareOrdinal(Undelayed, other.Undelayed);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((RicString) obj);
        }

        public override int GetHashCode() {
            return Undelayed.GetHashCode();
        }

        public static bool operator ==(RicString left, RicString right) {
            return Object.Equals(left, right);
        }

        public static bool operator !=(RicString left, RicString right) {
            return !Object.Equals(left, right);
        }

        public string Ric {
            get { return _ric; }
        }

        public string Undelayed {
            get { return _undelayed; }
        }

        public bool IsDelayed {
            get { return _isDelayed; }
        }

        public bool Equals(RicString x, RicString y) {
            return x == y;
        }

        public int GetHashCode(RicString obj) {
            return obj.GetHashCode();
        }
    }
}