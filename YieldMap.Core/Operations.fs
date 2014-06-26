namespace YieldMap.Core

module Operations =
    open YieldMap.Loader.SdkFactory

    open YieldMap.Requests

    open YieldMap.Transitive
    open YieldMap.Tools.Response
    open YieldMap.Tools.Logging

    open System
    open Manager

    let private logger = LogFactory.create "Operations"

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
            member x.Estimate () = Loader.estimate ()
            member x.Execute (r, ?t) = Loader.reload s r.Chains r.Force

    type EstablishConnection (f:EikonFactory) = 
        interface Operation<unit, unit> with
            member x.Estimate () = 15000 // fifteen seconds would suffice
            member x.Execute (_, ?timeout) = async { 
                let! res = 
                    match timeout with
                    | Some t -> f.Connect t
                    | _ -> f.Connect ()
                
                return 
                    match res with
                    | Ping.Failure f -> Tweet.Failure f
                    | Ok -> Answer ()
            }

    type Shutdown () = 
        interface Operation<unit, unit> with
            member x.Estimate () = 500 // half a second should be enough
            member x.Execute (_, _) = async { 
                do! Async.Sleep 500
                return Answer () 
            }