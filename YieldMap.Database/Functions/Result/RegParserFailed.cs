using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YieldMap.Language;

namespace YieldMap.Database.Functions.Result {
    public class RegParserFailed : RegFailure {
        private readonly Exceptions.GrammarException _innerException;

        public RegParserFailed(Exceptions.GrammarException innerException) {
            _innerException = innerException;
        }

        public override RegResultType FailureType {
            get { return RegResultType.ParserFailed;  }
        }

        public Exceptions.GrammarException Cause {
            get { return _innerException; }
        }
    }
}
