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
    public partial class Ric //: IObjectWithState
    {
        public Ric()
        {
            this.RicToChains = new HashSet<RicToChain>();
            this.Descriptions = new HashSet<Description>();
            this.Idxes = new HashSet<Idx>();
        }
    
        public long id { get; set; }
        public string Name { get; set; }
        public Nullable<long> id_Isin { get; set; }
        public Nullable<long> id_Feed { get; set; }
        public Nullable<long> id_FieldGroup { get; set; }
    	public Ric ToPocoSimple() {
    	    return new Ric {
    			id = this.id,
    			Name = this.Name,
    			id_Isin = this.id_Isin,
    			id_Feed = this.id_Feed,
    			id_FieldGroup = this.id_FieldGroup,
    		};
    	}
    
    	//public State State {get;set;}
    		
        public virtual Feed Feed { get; set; }
        public virtual Isin Isin { get; set; }
        public virtual ICollection<RicToChain> RicToChains { get; set; }
        public virtual ICollection<Description> Descriptions { get; set; }
        public virtual FieldGroup FieldGroup { get; set; }
        public virtual ICollection<Idx> Idxes { get; set; }
    }
}
