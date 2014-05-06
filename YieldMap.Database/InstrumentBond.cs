//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace YieldMap.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class InstrumentBond
    {
        public InstrumentBond()
        {
            this.Ratings = new HashSet<Rating>();
        }
    
        public long id { get; set; }
        public Nullable<long> id_Issuer { get; set; }
        public Nullable<long> id_Borrower { get; set; }
        public Nullable<long> id_Currency { get; set; }
        public string BondStructure { get; set; }
        public string RateStructure { get; set; }
        public Nullable<long> IssueSize { get; set; }
        public string Name { get; set; }
        public Nullable<bool> IsCallable { get; set; }
        public Nullable<bool> IsPutable { get; set; }
        public string Series { get; set; }
        public Nullable<long> id_Isin { get; set; }
        public Nullable<long> id_Ric { get; set; }
        public Nullable<long> id_Ticker { get; set; }
        public Nullable<long> id_SubIndustry { get; set; }
        public Nullable<System.DateTime> Issue { get; set; }
        public Nullable<System.DateTime> Maturity { get; set; }
        public Nullable<long> id_Seniority { get; set; }
        public Nullable<long> id_Specimen { get; set; }
        public Nullable<System.DateTime> NextCoupon { get; set; }
        public Nullable<double> Coupon { get; set; }
    
        public virtual Borrower Borrower { get; set; }
        public virtual Currency Currency { get; set; }
        public virtual Seniority Seniority { get; set; }
        public virtual Issuer Issuer { get; set; }
        public virtual Isin Isin { get; set; }
        public virtual Ric Ric { get; set; }
        public virtual Ticker Ticker { get; set; }
        public virtual SubIndustry SubIndustry { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual Specimen Specimen { get; set; }
    }
}
