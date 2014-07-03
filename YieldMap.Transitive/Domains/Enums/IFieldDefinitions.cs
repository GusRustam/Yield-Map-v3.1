using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Enums {
    public interface IFieldDefinitions {
        FieldDefinition Bid { get; }
        FieldDefinition Ask { get; }
        FieldDefinition Last { get; }
        FieldDefinition Close { get; }
        FieldDefinition Vwap { get; }
        FieldDefinition Volume { get; }
        FieldDefinition Value { get; }
        FieldDefinition Tenor { get; }
        FieldDefinition Maturity { get; }        
    }
}