using System;
using System.Collections.Generic;

namespace YieldMap.Transitive.Tools {
    public class RicString : IComparable<RicString>, IEqualityComparer<RicString> {
        private readonly string _original;
        private readonly string _undelayed;
        private readonly bool _isDelayed;
        private readonly string _delayed;

        public RicString(string original) {
            _original = original;
            if (string.IsNullOrWhiteSpace(original)) throw new ArgumentException("ric null or whitespace");
            _isDelayed = _original[0] == '/';
            _undelayed = _isDelayed ? _original.Substring(1) : _original;
            _delayed = IsDelayed ? _original : '/' + _original;
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

        public static implicit operator string(RicString str) {
            return str.Original;
        }

        public static bool operator ==(RicString left, RicString right) {
            return Object.Equals(left, right);
        }

        public static bool operator !=(RicString left, RicString right) {
            return !Object.Equals(left, right);
        }

        public string Original {
            get { return _original; }
        }

        public string Undelayed {
            get { return _undelayed; }
        }

        public bool IsDelayed {
            get { return _isDelayed; }
        }

        public string Delayed {
            get { return _delayed; }
        }

        public bool Equals(RicString x, RicString y) {
            return x == y;
        }

        public int GetHashCode(RicString obj) {
            return obj.GetHashCode();
        }
    }
}