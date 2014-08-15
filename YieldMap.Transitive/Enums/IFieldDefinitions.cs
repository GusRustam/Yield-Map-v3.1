using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public interface IFieldDefinitions {
        NFieldDefinition Bid { get; }
        NFieldDefinition Ask { get; }
        NFieldDefinition Last { get; }
        NFieldDefinition Close { get; }
        NFieldDefinition Vwap { get; }
        NFieldDefinition Volume { get; }
        NFieldDefinition Value { get; }
        NFieldDefinition Tenor { get; }
        NFieldDefinition Maturity { get; }        
    }
}