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
    //using YieldMap.Database.Domains;
    public partial class InstrumentType //: IObjectWithState
    {
        public InstrumentType()
        {
            this.Instruments = new HashSet<Instrument>();
            this.Properties = new HashSet<Property>();
        }
    
        public long id { get; set; }
        public string Name { get; set; }
    	public InstrumentType ToPocoSimple() {
    	    return new InstrumentType {
    			id = this.id,
    			Name = this.Name,
    		};
    	}
    
    	//public State State {get;set;}
    		
        public virtual ICollection<Instrument> Instruments { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
    }
}
