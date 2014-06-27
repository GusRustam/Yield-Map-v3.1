namespace YieldMap.Database.Functions.Result {
    public class RegSuccess : IRegResult {
        public RegResultType FailureType { get {return RegResultType.Ok;} }
        public override string ToString() {
            return "Success";
        }

        public bool Success() {
            return true;
        }
    }
}