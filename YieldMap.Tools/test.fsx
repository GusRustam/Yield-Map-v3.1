#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#endif

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

    type Success<'T> = 
        | Answer of 'T | Failure of Failure
        override x.ToString() = 
            match x with
            | Answer res -> sprintf "Answer %A" <| res.ToString()
            | Failure x -> sprintf "Failure %s" <| x.ToString()

    type private E<'T> = unit -> Success<'T>

    let exec (comp : E<'T>) : Success<'T>  = comp () 
    let private always y : E<'T> = fun () -> y
    let private fail res = Failure res
    let private bind expr comp = match exec expr with Failure f -> fail f | x -> comp x
    let private combine expr1 expr2 = always (match exec expr1 with Failure f -> expr2 | res -> res)
    let private tryFinally expr handler = try exec expr finally handler
    let private tryWith expr catcher = try exec expr with e -> catcher e

    type SoBuilder() = 
        member x.Delay expr = always <| exec expr
        member x.Bind (expr, comp) = bind expr comp
        member x.Return z = Answer z
        member x.ReturnFrom expr = exec expr
        member x.Combine (expr1, expr2) = combine expr1 expr2
        member x.TryFinally (expr, handler) = tryFinally expr handler
        member x.TryWith (expr, catcher) = tryWith expr catcher
        member x.Zero () = Success<_>.Failure <| Problem "Condition failure"

    let success = SoBuilder()

open Responses

let vova = success {
    return true
}
let vova1 = exec vova
printfn "%A" vova1

let dima = success {
    if 1 > 2 then return 22
}
printfn "%A" dima
let dima1 = exec dima
printfn "%A" dima1

let zoo = success {
    let! a = vova
    return a
}
printfn "%A" zoo
let zoo1 = exec zoo
printfn "%A" zoo1

let zoo2 = success {
    let! a = dima
    return a
}
printfn "%A" zoo2
let zoo12 = exec zoo
printfn "%A" zoo12

let bobo = success {
    return! vova
}
printfn "%A" bobo
let bobo1 = exec bobo
printfn "%A" bobo1
