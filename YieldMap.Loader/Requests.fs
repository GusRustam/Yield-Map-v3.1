namespace YieldMap.Loader.Requests

[<AutoOpen>]
module Requests =
    open EikonDesktopDataAPI

    open System
    open System.Globalization
    open System.Reflection

    open YieldMap.Requests.MetaTables
    open YieldMap.Requests.Tools.Attrs

    open YieldMap.Tools.Settings
    open YieldMap.Tools.Aux

    type ChainRequest = { Feed : string; Mode : string; Ric : string; Timeout : int }
        with static member create ric = { Feed = (!globalSettings).source.defaultFeed; Mode = ""; Ric = ric; Timeout = 0 }

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
            static member getAnswer x = match x with Answer m -> m | _ -> failwith "No Answer"

    type Chain = Answer of string array | Failed of exn