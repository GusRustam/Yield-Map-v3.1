namespace YieldMap.Transitive.Native.Entities {
    public class NRatingsView : INotIdentifyable {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id_Rating { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long id_RatingAgency { get; set; }

        [DbField(2)] // ReSharper disable InconsistentNaming once
        public long id_RatingAgencyCode { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public int RatingOrder { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public string RatingName { get; set; }

        [DbField(5)] // ReSharper disable once InconsistentNaming
        public string AgencyName { get; set; }

        [DbField(6)] // ReSharper disable once InconsistentNaming
        public string AgencyCode { get; set; }
    }
}