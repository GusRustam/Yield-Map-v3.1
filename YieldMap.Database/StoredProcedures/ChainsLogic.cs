using System;
using System.Collections.Generic;
using System.Linq;

namespace YieldMap.Database.StoredProcedures {
    internal static class Extensions {
        public static HashSet<T> Add<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Add(item));
            return res;
        }

        public static HashSet<T> Remove<T>(this HashSet<T> set, IEnumerable<T> items) {
            var res = set != null ? new HashSet<T>(set) : new HashSet<T>();
            items.ToList().ForEach(item => res.Remove(item));
            return res;
        }
    }

    internal class Set<T> {
        public static readonly Set<T> Empty = new Set<T>();
        private readonly HashSet<T> _data;

        public Set(IEnumerable<T> data) {
            _data = new HashSet<T>(data);
        }

        public Set() {
            _data = new HashSet<T>();
        }

        public IEnumerable<T> ToEnumerable() {
            return new HashSet<T>(_data);
        }

        public T[] ToArray() {
            return _data.ToArray();
        }

        public Set<T> Add(IEnumerable<T> items) {
            return new Set<T>(_data.Add(items));
        }

        public Set<T> Union(Set<T> another) {
            var res = new HashSet<T>(_data);
            res.UnionWith(another._data);
            return new Set<T>(res);
        }

        public Set<T> Intersect(Set<T> another) {
            var res = new HashSet<T>(_data);
            res.IntersectWith(another._data);
            return new Set<T>(res);
        }

        public Set<T> Subtract(Set<T> another) {
            var res = new HashSet<T>(_data);
            res = res.Remove(another._data);
            return new Set<T>(res);
        }

        public Set<T> SymmetricDiff(Set<T> another) {
            var res = new HashSet<T>(_data);
            res.SymmetricExceptWith(another._data);
            return new Set<T>(res);
        }

        public static Set<T> operator +(Set<T> x, Set<T> y) {
            if (x == null && y == null) return null;
            if (x == null) return y;
            return y == null ? x : x.Union(y);
        }

        public static Set<T> operator -(Set<T> x, Set<T> y) {
            if (x == null) return null;
            return y == null ? x : x.Subtract(y);
        }

        public static Set<T> operator *(Set<T> x, Set<T> y) {
            if (x == null || y == null) return null;
            return x.Intersect(y);
        }

        protected bool Equals(Set<T> other) {
            return Equals(_data, other._data);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Set<T>) obj);
        }

        public override int GetHashCode() {
            return (_data != null ? _data.GetHashCode() : 0);
        }

        public static bool Equals(Set<T> left, Set<T> right) {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
                return true;

            if (!ReferenceEquals(left, null) && !ReferenceEquals(right, null))
                return left.Equals(right);
            
            return 
                (ReferenceEquals(left, null) && ReferenceEquals(right, Empty)) || 
                (ReferenceEquals(right, null) && ReferenceEquals(left, Empty));
        }

        public static bool operator ==(Set<T> left, Set<T> right) {
            return Equals(left, right);
        }

        public static bool operator !=(Set<T> left, Set<T> right) {
            return !Equals(left, right);
        }

    }

    /// Some considerations:
    /// 
    /// BondRics = All rics from Table InstrumentBond
    ///
    /// BondRics =
    /// |--> Obsolete (those who have no bond, or those who have matured)
    /// |--> ToReload (those who need reloading)
    /// |--> Keep     (others)
    /// 
    /// ChainRics
    /// |--> New       |--> ToReload
    /// |--> Existing  |--> ToReload
    ///                |--> Keep
    /// 
    /// 
    public static class ChainsLogic {
        public static Dictionary<Mission, string[]> Classify(DateTime dt, string[] chainRics) {
            var res = new Dictionary<Mission, string[]>();

            var r = new Refresh();

            var existing = new Set<String>(r.AllBondRics().Select(x => x.Name));
            var obsolete = new Set<String>(r.ObsoleteBondRics(dt).Select(x => x.Name));
            var toReload = new Set<String>(r.StaleBondRics(dt).Select(x => x.Name));
            var incoming = new Set<String>(chainRics);

            res[Mission.ToReload] = (incoming - existing + toReload).ToArray();
            res[Mission.Obsolete] = obsolete.ToArray();
            res[Mission.Keep] = (existing - obsolete - toReload).ToArray();

            return res;
        }
    }
}