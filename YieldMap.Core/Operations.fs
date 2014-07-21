namespace YieldMap.Core

module Operations =
    open YieldMap.Loader.SdkFactory

    open YieldMap.Requests

    open YieldMap.Transitive
    open YieldMap.Tools.Response
    open YieldMap.Tools.Logging

    open System
    open DbManager

    type ('Request, 'Answer) Operation =
        abstract Estimate : unit -> int
        abstract Execute : 'Request * int option -> 'Answer Tweet Async

    module Operation =
        let estimate (o : Operation<_,_>) = function Some t -> t | _ -> o.Estimate ()
        let execute (o : Operation<unit,_>) t = o.Execute ((), Some t)

    type LoadAndSaveRequest = {
        Chains : ChainRequest array
        Force : bool
    }

    type LoadAndSave (s:Drivers) = 
        interface Operation<LoadAndSaveRequest, unit> with
            member __.Estimate () = Loader.estimate ()
            member __.Execute (r, _) = Loader.reload s r.Chains r.Force

    type EstablishConnection (f:EikonFactory) = 
        interface Operation<unit, unit> with
            member __.Estimate () = 15000 // fifteen seconds would suffice
            member __.Execute (_, ?timeout) = async { 
                let! res = 
                    match timeout with
                    | Some t -> f.Connect t
                    | _ -> f.Connect ()
                
                return 
                    match res with
                    | Ping.Failure f -> Tweet.Failure f
                    | _ -> Answer ()
            }

    type Shutdown () = 
        interface Operation<unit, unit> with
            member __.Estimate () = 500 // half a second should be enough
            member __.Execute (_, _) = async { 
                do! Async.Sleep 500
                return Answer () 
            }

    type Recalculate (s:Drivers) = 
        interface Operation<unit, unit> with
            member __.Estimate () = 5 * 60 * 1000 // 5 minutes are still a great estimate m'lord
            member __.Execute (_, _) = Recalculator.recalculate s.Database