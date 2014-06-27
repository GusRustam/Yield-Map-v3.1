namespace YieldMap.Database.Functions.Result {
    public class RegNoSuchKey : RegFailure {
        public override RegResultType FailureType {
            get { return RegResultType.KeyNotFound; }
        }
    }
}