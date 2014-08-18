using System;

namespace YieldMap.Transitive.Native.Entities {
    public class NDescription : IIdentifyable, IEquatable<NDescription> {
        [DbField(0)] // ReSharper disable once InconsistentNaming
        public long id { get; set; }

        [DbField(1)] // ReSharper disable once InconsistentNaming
        public long? id_Issuer { get; set; }

        [DbField(2)] // ReSharper disable once InconsistentNaming
        public long? id_Borrower { get; set; }

        [DbField(3)] // ReSharper disable once InconsistentNaming
        public string RateStructure { get; set; }

        [DbField(4)] // ReSharper disable once InconsistentNaming
        public long? IssueSize { get; set; }

        [DbField(5)] // ReSharper disable once InconsistentNaming
        public string Series { get; set; }

        [DbField(6)] // ReSharper disable once InconsistentNaming
        public long? id_Isin { get; set; }

        [DbField(7)] // ReSharper disable once InconsistentNaming
        public long? id_Ric { get; set; }

        [DbField(8)] // ReSharper disable once InconsistentNaming
        public long? id_Ticker { get; set; }

        [DbField(9)] // ReSharper disable once InconsistentNaming
        public long? id_SubIndustry { get; set; }

        [DbField(10)] // ReSharper disable once InconsistentNaming
        public long? id_Specimen { get; set; }

        [DbField(11)] // ReSharper disable once InconsistentNaming
        public DateTime? Issue { get; set; }

        [DbField(12)] // ReSharper disable once InconsistentNaming
        public DateTime? Maturity { get; set; }

        [DbField(13)] // ReSharper disable once InconsistentNaming
        public long? id_Seniority { get; set; }

        [DbField(14)] // ReSharper disable once InconsistentNaming
        public DateTime? NextCoupon { get; set; }

        public override int GetHashCode() {
            return (int)id;
        }

        public bool Equals(NDescription other) {
            if (other == null)
                return false;

            if (id != default(long) && other.id != default(long) && id == other.id)
                return true;

            return id_Issuer == other.id_Issuer && id_Borrower == other.id_Borrower && 
                   RateStructure == other.RateStructure && IssueSize == other.IssueSize &&
                   Series == other.Series && id_Isin == other.id_Isin && id_Ric == other.id_Ric && 
                   id_Ticker == other.id_Ticker && Issue == other.Issue && 
                   Maturity == other.Maturity && id_Seniority == other.id_Seniority && 
                   NextCoupon == other.NextCoupon;
        }
    }
}