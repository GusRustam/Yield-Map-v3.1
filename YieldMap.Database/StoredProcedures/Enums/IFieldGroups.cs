namespace YieldMap.Database.StoredProcedures.Enums {
    public interface IFieldGroups {
        FieldGroup Default { get; }
        FieldGroup Micex { get; }
        FieldGroup Eurobonds { get; }
    }
}