namespace YieldMap.Core.Application


[<AutoOpen>]
module internal Timeouts =
    // todo default timeouts
    type Timeouts = {
        load : int
        connect : int
        agent : int
        awaiter : int
    }

    let timeouts = { load = 5000; connect = 2000; agent = 1000; awaiter = 100 }

[<AutoOpen>]
module Package = 
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    
    open System

    type Operations = {
        TodayFix : DateTime
//        Saver : SavingOperations
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
    }

[<RequireQualifiedAccess>]
module ExternalOperations =
    open YieldMap.Core.Responses

    open YieldMap.Loader.Requests
    open YieldMap.Loader.SdkFactory
    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging

    let private logger = LogFactory.create "Operations"

    [<RequireQualifiedAccess>]
    module private Connecting = 
        let private (|TimedOut|Established|Failed|) = function
            | Some response ->
                match response with
                | Connection.Connected -> Established
                | Connection.Failed e -> Failed e
            | None -> TimedOut

        let connect (f:EikonFactory) = async { 
            let! res = f.Connect () |> Async.WithTimeout (Some timeouts.connect) 
            return
                match res with
                | TimedOut -> Success.Failure Failure.Timeout
                | Failed e -> Success.Failure <| Failure.Error e
                | Established -> Success.Ok
        }

    module Loading = 
        open YieldMap.Core.Responses
        open YieldMap.Core.Notifier

        open YieldMap.Database
        open YieldMap.Database.StoredProcedures

        open YieldMap.Loader.MetaChains
        
        open YieldMap.Requests.MetaTables

        open YieldMap.Tools.Aux
        open YieldMap.Tools.Aux.Workflows.Attempt
        open YieldMap.Tools.Location
        open YieldMap.Tools.Logging

        open System
        open System.Collections.Generic
        open System.IO

        let private logger = LogFactory.create "Loading"

        let (|ChainAnswer|ChainFailure|) = function
            | Choice1Of2 ch ->
                match ch with 
                | Chain.Answer a -> ChainAnswer a
                | Chain.Failed u -> ChainFailure u
            | Choice2Of2 ex -> ChainFailure ex

        let loadChains (m:ChainMetaLoader) chains = async {
            let names = chains |> Seq.map (fun r -> r.Ric) |> Array.ofSeq
            let! results = 
                chains 
                |> Seq.map (fun request -> m.LoadChain request |> Async.Catch)
                |> Async.Parallel
            
            let results = results |> Array.zip names

            let rics = results |> Array.choose (fun (ric, res) -> match res with ChainAnswer a -> Some (ric, a) | _ -> None)
            let fails = results |> Array.choose (fun (ric, res) -> match res with ChainFailure e -> Some (ric, e) | _ -> None)
                    
            // todo some better reporting
            do fails |> Array.iter (fun (ric, e) -> logger.WarnF "Failed to load chain %s because of %s" ric (e.ToString()))
            
            return rics, fails
        }

        type Metabuilder () = 
            member x.Bind (operation, rest) = 
                async {
                    let! res = operation
                    match res with 
                    | Meta.Answer a -> return! rest a
                    | Meta.Failed e -> return Some e
                }
            member x.Return (res : unit option) = async { return res }
            member x.Zero () = async { return None }

        let meta = Metabuilder ()

        let loadAndSaveMetadata (s:Operations) rics = meta {
            let loader = s.Loader

            let! bonds = loader.LoadMetadata<BondDescr> rics
            Additions.SaveBonds bonds

            // TODO save chains
            let! frns = loader.LoadMetadata<FrnData> rics
            Additions.SaveFrns frns

            let! issueRatings = loader.LoadMetadata<IssueRatingData> rics
            Additions.SaveIssueRatings issueRatings
                            
            let! issuerRatings = loader.LoadMetadata<IssuerRatingData> rics
            Additions.SaveIssuerRatings issuerRatings
        }

        let rec reload (s:Operations) chains force  = 
            let loader, dt = s.Loader, s.TodayFix

            logger.Trace "reload ()"
            async {
                if force || force && Refresh.NeedsReload(s.TodayFix)  then
                    try
                        BackupRestore.Backup ()
                        return! load s chains
                    with e -> 
                        logger.ErrorEx "Load failed" e
                        return! loadFailed s e
                else return Ok
            }

         and private load (s:Operations) requests = 
            logger.Trace "load ()"
            async {
                try
                    let! ricsByChain, fails = loadChains s.Loader requests

                    // reporting errors
                    fails |> Array.iter (fun (ric, e) -> 
                        Notifier.notify ("Loading", Problem <| sprintf "Failed to load chain %s because of %s" ric (e.ToString()), Severity.Warn))
                    
                    // saving rics and chains
                    ricsByChain |> Array.iter (fun (chain, rics) -> Additions.SaveChainRics(chain, rics))

                    // extracting rics
                    let chainRics = ricsByChain |> Array.map snd |> Array.collect id |> set
                    
                    // now determine which rics to reload and refresh
                    let classified = ChainsLogic.Classify (s.TodayFix, chainRics |> Set.toArray)

                    logger.InfoF "Will reload %d, kill %d and keep %d rics" 
                        (classified.[Mission.ToReload].Length) 
                        (classified.[Mission.Obsolete].Length) 
                        (classified.[Mission.Keep].Length)

                    // todo delete obsolete rics <- definitely a stored procedure // todo should I do a cleanup here?
                    try Refresh.DeleteBonds <| HashSet<_>(classified.[Mission.Keep])
                    with e -> logger.ErrorEx "Failed to cleanup" e
                    
                    let! res = loadAndSaveMetadata s classified.[Mission.ToReload]
                    match res with 
                    | Some e -> return! loadFailed s e
                    | None -> 
                        BackupRestore.Cleanup ()
                        return Ok
                with e -> 
                    logger.ErrorEx "Load failed" e
                    return! loadFailed s e
            }

        and private loadFailed (s:Operations) (e:exn) = 
            logger.Trace "loadFailed ()"
            async {
                try 
                    BackupRestore.Restore ()
                    logger.ErrorEx "Failed to reload data, restored successfully" e
                    return Failure (Problem "Failed to reload data, restored successfully")
                with e ->
                    logger.ErrorEx "Failed to reload and restore data" e
                    return Failure (Problem "Failed to reload and restore data")
            }

    // todo more advanced evaluation !!!
    let expectedLoadTime = timeouts.load
    let expectedConnectTime = timeouts.connect

    let private asSuccess timeout work = 
        work 
        >> Async.WithTimeout (Some timeout)
        >> Async.Map (function Some x -> x | None -> Failure Timeout)
    
    let load c f = asSuccess expectedLoadTime (Loading.reload c f)
    let connect = Connecting.connect |> asSuccess expectedConnectTime

module AnotherStartup =
    open YieldMap.Core.Notifier
    open YieldMap.Core.Portfolio
    open YieldMap.Core.Responses
    
    open YieldMap.Database.StoredProcedures

    open YieldMap.Loader
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.Requests

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging

    open System
    open System.Threading

    let logger = LogFactory.create "Startup"

    type State = Started | Connected | Initialized | Closed

    type Status =
        | State of State 
        | NotResponding
        override x.ToString () = 
            match x with
            | State state -> sprintf "State %A" state
            | NotResponding -> "NotResponding"

    type Commands = 
        | Connect of State AsyncReplyChannel
        | Reload of bool * State AsyncReplyChannel
        | Close of State AsyncReplyChannel
        override x.ToString () = 
            match x with
            | Connect _ -> "Connect"
            | Reload _ -> "Reload"
            | Close _ -> "Close"

    type Startup (q:Operations)  = 
        let s = Event<_> ()

        let f = q.Factory
        let c = q.Calendar
        let m = q.Loader
        let dt = c.Today

        do f.OnConnectionStatus |> Observable.add (fun state -> 
            match state with 
            | Connection.Failed e -> () 
            | Connection.Connected -> ()) // TODO ON DISCONNECT / RECONNECT DO SOMETHING (IF NECESSARY :))

        let a = MailboxProcessor.Start (fun inbox ->
            let rec started (channel : State AsyncReplyChannel option) = 
                logger.Debug "[--> started ()]"

                async {
                    s.Trigger Started
                    match channel with Some che -> che.Reply Started | None -> ()

                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Started: message %s]" (cmd.ToString())
                    match cmd with
                    | Connect channel -> 
                        let! res = ExternalOperations.connect f
                        match res with
                        | Success.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! started (Some channel)
                        | Success.Ok -> return! connected channel
                    | Reload (force, channel) -> 
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Started" (cmd.ToString()), Severity.Warn)
                        return! started (Some channel) 
                    | Close channel -> return close channel
                }

            and connected (channel : State AsyncReplyChannel) = 
                logger.Debug "[--> connected ()]"

                async {
                    s.Trigger Connected
                    channel.Reply Connected

                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Connected: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect channel -> 
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Connected" (cmd.ToString()), Severity.Warn)
                        return! connected channel 
                    | Reload (_, channel) -> // ignoring force parameter on primary loading
                        logger.Debug "[Primary reload]"
                        
                        let y = 
                            Refresh.ChainsInNeed c.Today 
                            |> Seq.map (fun r -> { Ric = r.Name; Feed = r.Feed.Name; Mode = r.Params; Timeout = 0}) // todo timeout

                        let! res = ExternalOperations.load q y true
                        match res with
                        | Success.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! connected channel
                        | Success.Ok ->  return! initialized channel
                    | Close channel -> return close channel
                }

            and initialized (channel : State AsyncReplyChannel) = 
                logger.Debug "[--> initialized ()]"

                async {
                    s.Trigger Initialized
                    channel.Reply Initialized
                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Initialized: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect channel ->
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Initialized" (cmd.ToString()), Severity.Warn)
                        return! initialized channel 
                    | Reload (force, channel) ->
                        logger.Debug "[Secondary reload]"
                        let x = []                                                      // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        let! res = ExternalOperations.load q x force
                        match res with
                        | Success.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! connected channel
                        | Success.Ok ->  return! initialized channel
                    | Close channel -> return close channel
                }

            and failed e = 
                logger.Debug "[--> failed ()]"
                Notifier.notify ("Startup", Error e, Severity.Evil)
                s.Trigger Closed

            and close channel = 
                logger.Debug "[--> closed ()]"
                s.Trigger Closed
                channel.Reply Closed

            async {
                let! res = started None |> Async.Catch
                match res with
                | Choice2Of2 e -> return failed e
                | _ -> return ()
            }
        )

        let tryCommand command timeout = async {
            let! answer = a.PostAndTryAsyncReply (command, timeout)
            match answer with
            | Some state -> return State state
            | None -> return NotResponding
        }        

        member x.StateChanged = s.Publish

        member x.Connect () = tryCommand Commands.Connect (ExternalOperations.expectedConnectTime + timeouts.agent)
        member x.Reload force = tryCommand (fun channel -> Commands.Reload (force, channel)) (ExternalOperations.expectedLoadTime + timeouts.agent)
        member x.Close () = tryCommand Commands.Close timeouts.agent