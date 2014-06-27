using YieldMap.Language;

namespace YieldMap.Database.Functions.Result {
    public class RegParserFailed : RegFailure {
        private readonly Exceptions.GrammarException _innerException;

        public override string ToString() {
            return "Failed due to parser exception " + _innerException;
        }
        
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
