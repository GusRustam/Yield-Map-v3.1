using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public interface ISourceTypes {
        NSourceType Universe { get; }
        NSourceType Chain { get; }
        NSourceType List { get; }
        NSourceType Query { get; }
        NSourceType Source { get; }
    }
}