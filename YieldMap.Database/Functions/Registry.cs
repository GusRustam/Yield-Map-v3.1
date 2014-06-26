using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using YieldMap.Language;

namespace YieldMap.Database.Functions {
    using Val = Lexan.Value;

    public class Grammar {
        private readonly IEnumerable<Syntan.Syntel> _syntax;
        private readonly string _expression;

        public IEnumerable<Syntan.Syntel> Syntax {
            get { return _syntax; }
        }

        public string Expression {
            get { return _expression; }
        }

        public Grammar(string expression) {
            _expression = expression;
            _syntax = Syntan.grammarize(_expression);
        }
    }

    public enum RegResultType {
        Ok,
        KeyNotFound,
        InterpreterFailed
    }

    public interface IRegResult {
        RegResultType FailureType { get; }
        bool Success();
    }

    public class RegSuccess : IRegResult {
        public RegResultType FailureType { get {return RegResultType.Ok;} }
        public bool Success() {
            return true;
        }
    }

    public abstract class RegFailure : IRegResult {
        public abstract RegResultType FailureType { get; }
        public bool Success() {
            return false;
        }
    }

    public class RegNoSuchKey : RegFailure {
        public override RegResultType FailureType {
            get { return RegResultType.KeyNotFound; }
        }
    }

    public class RegInterpreterFailed : RegFailure {
        private readonly Exceptions.InterpreterException _innerException;

        public RegInterpreterFailed(Exceptions.InterpreterException innerException) {
            _innerException = innerException;
        }

        public override RegResultType FailureType {
            get { return RegResultType.InterpreterFailed; }
        }

        public Exceptions.InterpreterException Cause {
            get { return _innerException; }
        }
    }


public class Registry {
        private static readonly ConcurrentDictionary<int, Grammar> Register = new ConcurrentDictionary<int, Grammar>();

        public bool Evaluate(int item, Dictionary<string, object> context, out Val val) {
            Grammar g;
            if (Register.TryGetValue(item, out g)) {
                try {
                    val = Interpreter.evaluate(g.Syntax, context);
                    return true;
                } catch (Exception e) {
                    val = null;
                    return false;
                }
            }
            val = null;
            return false;
        }
    }
}
