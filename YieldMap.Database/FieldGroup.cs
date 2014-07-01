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
            this.Rics = new HashSet<Ric>();
        }
    
        public long id { get; set; }
        public string Name { get; set; }
        public bool Default { get; set; }
        public Nullable<long> id_DefaultFieldDef { get; set; }
    	public FieldGroup ToPocoSimple() {
    	    return new FieldGroup {
    			id = this.id,
    			Name = this.Name,
    			Default = this.Default,
    			id_DefaultFieldDef = this.id_DefaultFieldDef,
    		};
    	}
    		
        public virtual ICollection<Field> Fields { get; set; }
        public virtual FieldDefinition FieldDefinition { get; set; }
        public virtual ICollection<Ric> Rics { get; set; }
    }
}
