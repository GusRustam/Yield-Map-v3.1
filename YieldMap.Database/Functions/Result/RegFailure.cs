namespace YieldMap.Database.Functions.Result {
    public class RegFailure : IRegResult {
        public virtual RegResultType FailureType { get { return RegResultType.Failed; } }
        public override string ToString() {
            return "Failure";
        }
        public bool Success() {
            return false;
        }
    }
}