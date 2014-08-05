namespace YieldMap.Transitive.Native.Entities {
    public class NInstrumentIBView : INotIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Ric { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_Instrument { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_Borrower { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public long? id_Issuer { get; set; }
        
        [DbField(4)] // ReSharper disable once InconsistentNaming
        public string Name { get; set; }
    }
}