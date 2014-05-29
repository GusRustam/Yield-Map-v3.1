#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#endif

////////////////////////////////////////////////////////////////////////////////
type LoggingBuilder() =
    let log p = printfn "expression is %A" p

    member this.Bind(x, f) = 
        printfn "Logging.Bind %A %A" x f
        log x
        f x

    member this.Return(x) = 
        printfn "Logging.Return %A" x
        x

let logger = new LoggingBuilder()

//------------------------------
let loggedWorkflow = logger {
    let! x = 42
    let! y = 43
    let! z = x + y
    return z
}


/////////////////////////////////////////////////////////////////////////////////
type State<'a, 's> = State of ('s -> 'a * 's)

let runState (State s) a = s a
let getState = State (fun s -> (s,s))
let putState s = State (fun _ -> ((),s))

type StateBuilder() =
    member this.Return(a) = 
        printfn "State.Return %A" a
        State (fun s -> (a,s))

    member this.Bind(m,k) =
        printfn "State.Bind %A %A" m k
        State (fun s -> 
            let (a,s') = runState m s 
            runState (k a) s')

    member this.ReturnFrom (m) = 
        printfn "State.ReturnFrom %A" m
        m

let state = new StateBuilder()

//------------------------------
let DoSomething counter = 
    printfn "DoSomething. Counter=%i " counter
    counter + 1

let FinalResult counter = 
    printfn "FinalResult. Counter=%i " counter
    counter

// convert old functions to "state-aware" functions
let lift f = state {
    let! s = getState 
    return! putState (f s)
}

// new functions
let DoSomething' = lift DoSomething
let FinalResult' = lift FinalResult

let counterWorkflow = 
    let s = state {
        do! DoSomething'
        do! DoSomething'
        do! DoSomething'
        do! FinalResult'
    } 
    runState s 0


/////////////////////////////////////////////////////////////////////////////////
type MaybeBuilder() =
    member this.Bind(x, f) = 
        printfn "Maybe.Bind %A %A" x f
        match x with
        | None -> None
        | Some a -> f a

    member this.Return(x) = 
        printfn "Maybe.Return %A" x
        Some x

    member this.ReturnFrom(x) = 
        printfn "Maybe.ReturnFrom %A" x
        x // not Some x, but x itself. It has to be 'a Option, yet it is not obvious from the code
   
let maybe = new MaybeBuilder()

//------------------------------
let divideBy bottom top =
    if bottom = 0 then None else Some(top/bottom)

let divideByWorkflow init x y z = maybe {
    let! a = init |> divideBy x
    let! b = a |> divideBy y
    let! c = b |> divideBy z
    return c
}  

let divideByWorkflow2 init x y z = maybe {
    let! a = init |> divideBy x
    let! b = a |> divideBy y
    return! b |> divideBy z
}    

let good = divideByWorkflow 12 3 2 1
let bad = divideByWorkflow 12 3 0 1

let good1 = divideByWorkflow2 12 3 2 1
let bad1 = divideByWorkflow2 12 3 0 1

/////////////////////////////////////////////////////////////////////////////////
type OrElseBuilder() =
    member this.ReturnFrom(x) = 
        printfn "OrElse.ReturnFrom %A" x
        x

    member this.Combine (a,b) = 
        printfn "OrElse.Combine %A %A" a b
        match a with
        | Some _ -> a  // a succeeds -- use it
        | None -> b    // a fails -- use b instead

    member this.Delay(f) = 
        printfn "OrElse.Delay %A" f
        f()

let orElse = new OrElseBuilder()

//------------------------------
let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList
let map3 = [ ("CA","California"); ("NY","New York") ] |> Map.ofList

let multiLookup key = orElse {
    return! map1.TryFind key
    return! map2.TryFind key
    return! map3.TryFind key
}

multiLookup "1" |> printfn "Result for A is %A" 
multiLookup "A" |> printfn "Result for A is %A" 
multiLookup "CA" |> printfn "Result for CA is %A" 
multiLookup "X" |> printfn "Result for X is %A" 