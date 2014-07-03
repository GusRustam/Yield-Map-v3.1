namespace YieldMap.Transitive.Domains.Enums {
    /// <summary>
    /// Represents internal names of fields for given
    /// </summary>
    public interface IFieldSet {
        string Bid { get; }
        string Ask { get; }
        string Last { get; }
        string Close { get; }
        string Vwap { get; }
        string Volume { get; }
        string Value { get; }
        string Tenor { get; }
        string Maturity { get; }
    }
}