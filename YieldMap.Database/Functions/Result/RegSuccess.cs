namespace YieldMap.Database.Functions.Result {
    public class RegSuccess : IRegResult {
        public RegResultType FailureType { get {return RegResultType.Ok;} }
        public bool Success() {
            return true;
        }
    }
}