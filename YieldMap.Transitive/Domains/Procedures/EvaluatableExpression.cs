using System.Collections.Generic;
using YieldMap.Language;

namespace YieldMap.Transitive.Domains.Procedures {
    internal class EvaluatableExpression {
        private readonly string _expression;
        private readonly IEnumerable<Syntan.Syntel> _grammar;

        public override string ToString() {
            return string.Format("Expr {0}", Expression);
        }

        public EvaluatableExpression(string expression) {
            _expression = expression;
            _grammar = Syntan.grammarize(expression);
        }

        public string Expression {
            get { return _expression; }
        }

        public IEnumerable<Syntan.Syntel> Grammar {
            get { return _grammar; }
        }
    }
}