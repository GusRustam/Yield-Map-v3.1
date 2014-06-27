using YieldMap.Language;

namespace YieldMap.Database.Functions.Result {
    public static class RegResult {
        public static IRegResult Success {
            get {
                return new RegSuccess();
            }
        }

        public static IRegResult NoSuchKey {
            get {
                return new RegNoSuchKey();
            }
        }

        public static IRegResult Failure {
            get {
                return new RegFailure();
            } 
        }

        public static IRegResult InterpreterFailed(Exceptions.InterpreterException e) {
            return new RegInterpreterFailed(e);
        }

        public static IRegResult ParserFailed(Exceptions.GrammarException e) {
            return new RegParserFailed(e);
        }
    }
}