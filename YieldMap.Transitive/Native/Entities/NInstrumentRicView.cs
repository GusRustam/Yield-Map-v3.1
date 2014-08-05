namespace YieldMap.Transitive.Native.Entities {
    public class NInstrumentRicView : INotIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Ric { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }

        [DbField(2)] // ReSharper disable InconsistentNaming once
        public long id_Description { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }
    }
}