namespace YieldMap.Core.Responses

[<AutoOpen>]
module Responses =
    type private FailureStatic = Failure
    and Failure = 
        | Problem of string | Error of exn | Timeout
        static member toString x = 
            match x with
            | Problem str -> sprintf "Problem %s" str
            | Error e -> sprintf "Error %s" (e.ToString())
            | Timeout -> "Timeout"
        override x.ToString() = FailureStatic.toString x

    type Success = 
        Ok | Failure of Failure
        override x.ToString() = 
            match x with
            | Ok -> "OK"
            | Failure x -> sprintf "Failure %s" <| x.ToString()
