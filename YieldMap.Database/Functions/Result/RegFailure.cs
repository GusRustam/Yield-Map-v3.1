namespace YieldMap.Database.Functions.Result {
    public class RegFailure : IRegResult {
        public virtual RegResultType FailureType { get { return RegResultType.Failed; } }
        public bool Success() {
            return false;
        }
    }
}