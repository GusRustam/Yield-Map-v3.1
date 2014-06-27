using System.Collections.Generic;
using YieldMap.Language;

namespace YieldMap.Database.Functions {
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
}