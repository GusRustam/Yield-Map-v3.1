using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public interface ILegTypes {
        NLegType Paid { get; }
        NLegType Received { get; }
        NLegType Both { get; }
    }
}