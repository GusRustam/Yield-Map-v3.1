using YieldMap.Database;

namespace YieldMap.Transitive.Domains.Enums {
    public interface IFieldGroups {
        FieldGroup Default { get; }
        FieldGroup Micex { get; }
        FieldGroup Eurobonds { get; }
        FieldGroup RussiaCpi { get; }
        FieldGroup Mosprime { get; }
        FieldGroup Swaps { get; }
    }
}