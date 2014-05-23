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
    
    public partial class FieldGroup
    {
        public FieldGroup()
        {
            this.Fields = new HashSet<Field>();
            this.Indices = new HashSet<Index>();
        }
    
        public long id { get; set; }
        public string Name { get; set; }
        public string DefaultField { get; set; }
        public bool Default { get; set; }
    	public FieldGroup ToPocoSimple() {
    	    return new FieldGroup {
    			id = this.id,
    			Name = this.Name,
    			DefaultField = this.DefaultField,
    			Default = this.Default,
    		};
    	}
    		
        public virtual ICollection<Field> Fields { get; set; }
        public virtual ICollection<Index> Indices { get; set; }
    }
}
