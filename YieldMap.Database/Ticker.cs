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
    
    public partial class Ticker
    {
        public Ticker()
        {
            this.InstrumentBonds = new HashSet<InstrumentBond>();
            this.Child = new HashSet<Ticker>();
        }
    
        public long id { get; set; }
        public string Name { get; set; }
        public Nullable<long> id_ParentTicker { get; set; }
    
        public virtual ICollection<InstrumentBond> InstrumentBonds { get; set; }
        public virtual ICollection<Ticker> Child { get; set; }
        public virtual Ticker Parent { get; set; }
    }
}
