namespace YieldMap.Requests.Tools

module Converters =
    open System
    open System.Globalization

    (* Converters *)
    type Cnv = abstract member Convert : string -> obj option

    type BoolConverter() = interface Cnv with member self.Convert x = Some <| box (string(x.[0]).ToUpper() = "Y")
    type NotNullConverter() = interface Cnv with member self.Convert x = if x = null then None else Some <| box x 
                
    type NotNullOrEmptyConverter() = 
        interface Cnv with 
            member self.Convert x = 
                if x = null then None
                elif x.Trim() <> String.Empty then Some <| box x 
                else None

    type DateConverter() = 
        interface Cnv with
            member self.Convert x = 
                let success, date = DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None)
                Some <| if success then box date else box x // чтобы работало и рейтеровскими данными и с моими

    type SomeFloatConverter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then Some <| box num else None

    type SomeInt64Converter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Int64.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then Some <| box num else None

module Attrs =
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
