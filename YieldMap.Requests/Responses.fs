namespace YieldMap.Requests

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


    type 'T Tweet = 
        Answer of 'T | Failure of Failure
        override x.ToString() = 
            match x with
            | Answer x -> sprintf "Answer %A" x
            | Failure e -> sprintf "Failure %s" <| e.ToString()
        static member isAnswer (x : _ Tweet) = match x with Answer _ -> true | _ -> false
        static member getAnswer (x : _ Tweet) = match x with Answer z -> z | _ -> failwith "No answer"

    type Ping = unit Tweet

    type TweetBuilder () = 
        member x.Bind (operation, rest) = 
            async {
                let! res = operation
                match res with 
                | Answer a -> return! rest a
                | Failure e -> return Some e
            }
        member x.Return (res : unit option) = async { return res }
        member x.Zero () = async { return None }

        // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
        member x.Using (disposable, body) = async {
            use d = disposable
            return! body d
        }

    let tweet = TweetBuilder ()