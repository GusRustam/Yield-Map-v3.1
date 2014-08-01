namespace YieldMap.Transitive.Native.Entities {
    public interface ITypedInstrument : INotIdentifyable {
        // ReSharper disable once InconsistentNaming
        long id_Instrument { get; set; }

        // ReSharper disable once InconsistentNaming
        long id_InstrumentType { get; set; }        
    }
}