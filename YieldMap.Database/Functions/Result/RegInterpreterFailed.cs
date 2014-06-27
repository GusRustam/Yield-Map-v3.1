using YieldMap.Language;

namespace YieldMap.Database.Functions.Result {
    public class RegInterpreterFailed : RegFailure {
        private readonly Exceptions.InterpreterException _innerException;
        public override string ToString() {
            return "Failed due to interpreter exception " + _innerException;
        }
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
}