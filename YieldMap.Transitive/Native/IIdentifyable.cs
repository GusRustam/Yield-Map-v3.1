namespace YieldMap.Transitive.Native {
    public interface IIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        long id { get; set; }
    }
}