namespace YieldMap.Requests

module Attributes =
    open System

    (* Request attributes *) 
    type RequestAttribute = 
        inherit Attribute
        val Request : string
        val Display : string
        new (display) = {Request = String.Empty; Display = display}
        new (request, display) = {Request = request; Display = display}

    type FieldAttribute = 
        inherit Attribute
        val Order : int
        val Name : string
        val Converter : Type option 
    
        new(order) = {Order = order; Name = String.Empty; Converter = None}
        new(order, name) = {Order = order; Name = name; Converter = None}
        new(order, name, converter) = {Order = order; Name = name; Converter = Some(converter)}

        override self.ToString () = 
            let convName (cnv : Type option) = match cnv with Some(tp) -> tp.Name | None -> "None"
            sprintf "%d | %s | %s" self.Order self.Name (convName self.Converter)