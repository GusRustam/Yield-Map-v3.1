namespace YieldMap.Transitive.Native.Entities {
    public class NFieldVsGroup : INotIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_FieldGroup { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Field { get; set; }

        [DbField(2)] // ReSharper disable InconsistentNaming
        public long id_FieldDefinition { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? id_DefaultFieldDefinition { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public bool DefaultGroup { get; set; }

        [DbField(5)] // ReSharper disable once InconsistentNaming
        public string FieldGroupName { get; set; }

        [DbField(6)] // ReSharper disable once InconsistentNaming
        public string SystemName { get; set; }

        [DbField(7)] // ReSharper disable once InconsistentNaming
        public string InternalName { get; set; }
    }
}