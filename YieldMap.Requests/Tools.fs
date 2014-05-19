namespace YieldMap.Requests

module Converters =
    open System
    open System.Globalization

    type Refinement = Invalid | Empty | Product of obj

    (* Converters *)
    type Cnv = abstract member Convert : string -> Refinement

    type RequiredBoolConverter() = interface Cnv with member self.Convert x = Product <| box (string(x.[0]).ToUpper() = "Y")
    type RequiredConverter() = interface Cnv with member self.Convert x = if x = null then Invalid else Product <| box x 
                
    type RequeredStringConverter() = 
        interface Cnv with 
            member self.Convert x = 
                if x = null then Invalid
                elif x.Trim() <> String.Empty then Product <| box x 
                else Invalid

    type OptionalDateConverter() = 
        interface Cnv with
            member self.Convert x = 
                let success, date = DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None)
                if success then Product <| box date else Empty

    type RequiredFloatConverter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then Product <| box num else Invalid

    type OptionalFloatConverter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then Product <| box num else Empty

    type OptionalInt64Converter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Int64.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then Product <| box num else Empty

[<AutoOpen>]
module Extensions =
    open System
    open System.Reflection
   
    /// Extension methods to get single attribute in a cleaner way
    type MemberInfo with
        member self.Attr<'T when 'T :> Attribute> () = 
            try Some(self.GetCustomAttribute(typedefof<'T>, false) :?> 'T) 
            with _ -> None

