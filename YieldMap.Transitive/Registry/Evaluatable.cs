using System;
using System.Collections.Generic;
using YieldMap.Language;


namespace YieldMap.Transitive.Registry {
    using Syntax = IEnumerable<Tuple<int, Syntan.Syntel>>;

    public class Evaluatable {
        private readonly string _expression;
        private readonly Syntax _grammar;

        public override string ToString() {
            return string.Format("Expr {0}", Expression);
        }

        public Evaluatable(string expression) {
            _expression = expression;
            _grammar = Syntan.grammarizeExtended(expression);
        }

        public string Expression {
            get { return _expression; }
        }

        public Syntax Grammar {
            get { return _grammar; }
        }
    }
}