using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using YieldMap.Tools.Logging;

namespace YieldMap.Database {
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

    public static class Tools {
        public static void Report(this Logging.Logger logger, string msg, DbEntityValidationException e) {
            logger.Error(msg);
            foreach (var eve in e.EntityValidationErrors) {
                logger.Error(
                    String.Format(
                        "Entity of type [{0}] in state [{1}] has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));

                foreach (var ve in eve.ValidationErrors)
                    logger.Error(String.Format("- Property: [{0}], Error: [{1}]", ve.PropertyName, ve.ErrorMessage));
            }
        }
    }
}
