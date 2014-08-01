using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Enums {
    public interface IFieldGroups {
        NFieldGroup Default { get; }
        NFieldGroup Micex { get; }
        NFieldGroup Eurobonds { get; }
        NFieldGroup RussiaCpi { get; }
        NFieldGroup Mosprime { get; }
        NFieldGroup Swaps { get; }
    }
}