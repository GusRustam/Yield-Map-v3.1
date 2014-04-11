namespace YieldMap.Loader.Requests

[<AutoOpen>]
module Requests =
    open EikonDesktopDataAPI

    open System
    open System.Globalization
    open System.Reflection

    open YieldMap.Tools.Settings
    open YieldMap.Tools.Aux

    type ChainRequest = { Feed : string; Mode : string; Ric : string; Timeout : int option }
        with static member create ric = { Feed = (!globalSettings).source.defaultFeed; Mode = ""; Ric = ric; Timeout = None }

    (* Converters *)
    type Cnv = abstract member Convert : string -> obj

    type BoolConverter() = 
        interface Cnv with 
            member self.Convert x = 
                box (string(x.[0]).ToUpper() = "Y")
    
    type NotNullConverter() = 
        interface Cnv with 
            member self.Convert x = 
                if x.Trim() <> String.Empty then box x else failwith "String empty"

    type DateConverter() = 
        interface Cnv with
            member self.Convert x = 
                let success, date = 
                    DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None)
                if success then date :> obj else x :> obj // чтобы работало и рейтеровскими данными и с моими

    type SomeFloatConverter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then box num else null

    type SomeInt64Converter() = 
        interface Cnv with
            member self.Convert x =
                let success, num = Int64.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture)
                if success then box num else null

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


    /// Parameters to make a request
    type MetaRequest = {
        Fields : string list
        Display : string
        Request : string
    } with 
        static member empty = { Fields = []; Display = ""; Request = "" }
        /// Creates MetaSetup object and some structure to parse and store data into T easily 
        static member extract<'T> () = 
            let def = typedefof<'T>
            match def.Attr<RequestAttribute>() with
            | Some x -> 
                let fields = 
                    def.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                    |> Array.map (fun p -> p.Attr<FieldAttribute>())
                    |> Array.choose (function | Some(x) when not <| String.IsNullOrEmpty(x.Name) -> Some(x.Name) | _ -> None)
                    |> List.ofArray
                {Request = x.Request; Display = x.Display;  Fields = fields}
            | None -> failwith "Invalid setup, no RequestAttribute"

    type Connection = Connected | Failed of exn
        with static member parse (e:EEikonStatus) = 
                match e with
                | EEikonStatus.Connected -> Connected
                | _ -> Failed <| exn (e.ToString())

    type Meta<'T> = 
        | Answer of 'T list 
        | Failed of exn
        with 
            static member isAnswer (m : Meta<'T>) = match m with Answer _ -> true | _ -> false
            static member getAnswer (Answer m) = m

    type Chain = Answer of string array | Failed of exn