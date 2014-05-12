namespace YieldMap.Requests

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

[<AutoOpen>]
module Extensions =
    open System
    open System.Reflection
   
    /// Extension methods to get single attribute in a cleaner way
    type MemberInfo with
        member self.Attr<'T when 'T :> Attribute> () = 
            try Some(self.GetCustomAttribute(typedefof<'T>, false) :?> 'T) 
            with _ -> None

