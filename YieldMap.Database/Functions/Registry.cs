using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using YieldMap.Database.Access;
using YieldMap.Database.Functions.Outcome;
using YieldMap.Database.Functions.Result;
using YieldMap.Database.Tools;
using YieldMap.Language;

namespace YieldMap.Database.Functions {
    public class Registry {
        private readonly IContainer _container;
        private static readonly ConcurrentDictionary<long, Grammar> Register = new ConcurrentDictionary<long, Grammar>();

        public Registry(IContainer container) {
            _container = container;
        }

        public void Refresh() {
            var conn = _container.Resolve<IDbConn>();
            var checkedIds = new Set<long>();
            using (var ctx = conn.CreateContext()) {
                foreach (var property in from p in ctx.Properties.ToList() where !String.IsNullOrWhiteSpace(p.Expression) select p) {
                    IRegResult res;

                    var id = property.id;
                    var expression = property.Expression;
                    
                    if (!Register.ContainsKey(id)) {
                        res = DoAdd(id, expression);
                    } else {
                        Grammar e;
                        if (Register.TryGetValue(id, out e)) 
                            res = e.Expression != expression ? DoAdd(id, expression) : RegResult.Success;
                        else res = DoAdd(id, expression);
                    }
                    checkedIds += property.id;
                    if (res != RegResult.Success) {
                        // todo log it
                    }
                }
            }

            foreach (var id in Register.Keys) {
                var res = RegResult.Success;
                if (!checkedIds.Contains(id)) res = Remove(id);
                if (res != RegResult.Success) {
                    // todo log it
                }
            }
        }

        private static IRegResult DoAdd(long id, string expression) {
            var grammar = new Grammar(expression);
            return Register.TryAdd(id, grammar) ? RegResult.Success : RegResult.Failure;
        }

        private IRegResult Remove(long id) {
            Grammar g;
            return Register.TryRemove(id, out g) ? RegResult.Success : RegResult.Failure;
        }

        public IRegResult Evaluate(int item, Dictionary<string, object> environment, out Computed val) {
            Grammar g;
            if (Register.TryGetValue(item, out g)) {
                try {
                    val = new Computed(Interpreter.evaluate(g.Syntax, environment));
                    return RegResult.Success;
                } catch (Exceptions.InterpreterException e) {
                    val = null;
                    return RegResult.InterpreterFailed(e);
                }
            }
            val = null;
            return RegResult.NoSuchKey;
        }
    }
}
