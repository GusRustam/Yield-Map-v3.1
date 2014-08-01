using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YieldMap.Transitive.Native.Variables {
    public class VariableHelper : IVariableHelper {
        private readonly Dictionary<Type, VariableRecord[]> _cache = new Dictionary<Type, VariableRecord[]>();

        public Dictionary<string, object> ToVariable<T>(T obj) {
            var type = typeof (T);
            if (!_cache.ContainsKey(type)) Cache(type);

            var res = new Dictionary<string, object>();
            foreach (var property in _cache[type]) {
                res[property.VarName] = property.Info.GetValue(obj);
            }
            return res;
        }

        private void Cache(Type type) {
            _cache[type] = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttributes<VariableAttribute>().Any())
                .Select(p => new VariableRecord(p)).ToArray();
        }
    }
}