namespace YieldMap.Transitive.Native.Entities {
    public interface IIdentifyable {
        // ReSharper disable once InconsistentNaming
        [DbField(0)]
        long id { get; set; }
    }
}