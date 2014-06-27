namespace YieldMap.Database.Functions.Result {
    public interface IRegResult {
        RegResultType FailureType { get; }
        bool Success();
    }
}