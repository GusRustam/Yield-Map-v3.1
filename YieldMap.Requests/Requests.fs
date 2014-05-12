namespace YieldMap.Requests

[<AutoOpen>]
module Requests =

    open EikonDesktopDataAPI

    open System
    open System.Globalization
    open System.Reflection

    open YieldMap.Requests.Attributes
    open YieldMap.Requests.MetaTables
    open YieldMap.Requests
    open YieldMap.Requests.Responses

    type ChainRequest = { Feed : string; Mode : string; Ric : string; Timeout : int }

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

    module Connection = 
        let parse (e:EEikonStatus) =
            match e with
            | EEikonStatus.Connected -> Ping.Answer ()
            | _ -> Ping.Failure <| Failure.Problem (e.ToString())