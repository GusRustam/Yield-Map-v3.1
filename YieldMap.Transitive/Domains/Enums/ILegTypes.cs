using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Enums {
    public interface ILegTypes {
        LegType Paid { get; }
        LegType Received { get; }
        LegType Both { get; }
    }
}