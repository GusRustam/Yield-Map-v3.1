using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Language;
using YieldMap.Tools.Logging;

namespace YieldMap.Transitive.Registry {
    public class FunctionRegistry : IFunctionRegistry {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Transient.FunctionRegistry");
        private readonly ConcurrentDictionary<long, Evaluatable> _registry = 
            new ConcurrentDictionary<long, Evaluatable>();

        public void Clear() {
            _registry.Clear();
        }

        public void Add(IEnumerable<Tuple<long, string>> items) {
            items.ToList().ForEach(item => {
                var propId = item.Item1;
                var expr = item.Item2;

                if (_registry.ContainsKey(propId)) {
                    Evaluatable value;
                    if (_registry.TryGetValue(propId, out value) &&
                        (value.Expression != expr && !_registry.TryRemove(propId, out value)))
                        Logger.Warn(string.Format("Failed to remove obsolete item with id {0}", propId));
                }

                if (!string.IsNullOrWhiteSpace(expr)) {
                    try {
                        var x = new Evaluatable(expr);
                        if (!_registry.TryAdd(propId, x)) 
                            Logger.Warn(string.Format("Failed to add an expression for property {0} to registry {1}", propId, x));
                    } catch (Exceptions.GrammarException e) {
                        Logger.WarnEx("Failed to parse", e);
                    }
                }
            });
        }

        public Lexan.Value Evaluate(long propertyId, Dictionary<string, object> env) {
            return Interpreter.evaluate(_registry[propertyId].Grammar, env);
        }

        public IEnumerable<Tuple<long, Lexan.Value>> EvaluateAll(Dictionary<string, object> env) {
            return _registry.Select(e => Tuple.Create(e.Key, Evaluate(e.Key, env)));
        }

        public Dictionary<long, Evaluatable> Items { get{return new Dictionary<long, Evaluatable>(_registry);}}
    }
}
