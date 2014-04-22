namespace YieldMap.Core.Application

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

[<RequireQualifiedAccess>]
module ExternalOperations =
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
        open YieldMap.Database
        open YieldMap.Loader.MetaChains
        open YieldMap.Loader.MetaTables

        open YieldMap.Tools.Aux
        open YieldMap.Tools.Aux.Workflows.Attempt
        open YieldMap.Tools.Location
        open YieldMap.Tools.Logging

        open System.IO

        let private logger = LogFactory.create "Loading"

        do MainEntities.SetVariable("PathToTheDatabase", Location.path)
        let private cnnStr = MainEntities.GetConnectionString("TheMainEntities")

        exception DbException of Failure

        let (|ChainAnswer|ChainFailure|) = function
            | Choice1Of2 ch ->
                match ch with 
                | Chain.Answer a -> ChainAnswer a
                | Chain.Failed u -> ChainFailure u
            | Choice2Of2 ex -> ChainFailure ex

        type SavingOperations = 
            abstract Backup : unit -> unit
            abstract Restore : unit -> unit
            abstract Clear : unit -> unit
            abstract NeedsReload : unit -> bool
            abstract SaveBonds : BondDescr list -> unit
            abstract SaveIssueRatings : IssueRatingData list -> unit
            abstract SaveIssuerRatings : IssuerRatingData list -> unit
            abstract SaveFrns : FrnData list -> unit

        type DbSavingOperations () = 
            // TODO OTHER OPERATIONS!!!
            interface SavingOperations with
                member x.Backup () =
                    use ctx = new MainEntities (cnnStr)
                    let path = Path.Combine(Location.path, "main.bak")
                    try
                        if File.Exists(path) then File.Delete(path)
                        let sql = sprintf "BACKUP DATABASE main TO DISK='%s'" path
                        ctx.Database.ExecuteSqlCommand(sql) |> ignore
                        if not <| File.Exists(path) then raise (DbException <| Problem "No backup file found")
                    with e ->  raise (DbException <| Error e)

                member x.Restore () = 
                    use ctx = new MainEntities (cnnStr)
                    let path = Path.Combine(Location.path, "main.bak")
                    try
                        if not <| File.Exists(path) then raise <| DbException (Problem "No restore file found")
                        let sql = sprintf "RESTORE DATABASE main FROM DISK='%s'" path
                        ctx.Database.ExecuteSqlCommand(sql) |> ignore
                        if File.Exists(path) then File.Delete(path)
                    with
                        | :? DbException -> reraise ()
                        | e -> raise <| DbException (Error e)

                member x.Clear () = ()
                member x.NeedsReload () = false
                member x.SaveBonds bonds = ()
                member x.SaveIssueRatings issue = ()
                member x.SaveIssuerRatings issuer = ()
                member x.SaveFrns frns = ()

        let private saver = DbSavingOperations () :> SavingOperations

        let loadChains (m:ChainMetaLoader) chains = async {
            let names = chains |> List.map (fun r -> r.Ric) |> Array.ofList
            let! results = 
                chains 
                |> List.map (fun request -> m.LoadChain request |> Async.Catch)
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

        let flow (m:ChainMetaLoader) rics = meta {
            let! bonds = m.LoadMetadata<BondDescr> rics
            saver.SaveBonds bonds
                            
            let! frns = m.LoadMetadata<FrnData> rics
            saver.SaveFrns frns

            let! issueRatings = m.LoadMetadata<IssueRatingData> rics
            saver.SaveIssueRatings issueRatings
                            
            let! issuerRatings = m.LoadMetadata<IssuerRatingData> rics
            saver.SaveIssuerRatings issuerRatings
        }

//        and private load (m:ChainMetaLoader) requests = 
//            logger.Trace "load ()"
//            async {
//                try
//                    let! ricsByChain, fails = loadChains m requests
//                    let rics = ricsByChain |> Array.map snd |> Array.collect id
//                    let! bonds = m.LoadMetadata<BondDescr> rics
//                    match bonds with
//                    | Meta.Answer bd -> 
//                        saver.SaveBonds bd
//                        let! frns = m.LoadMetadata<FrnData> rics
//                        match frns with 
//                        | Meta.Answer fn ->
//                            saver.SaveFrns fn
//                            let! issueRatings = m.LoadMetadata<IssueRatingData> rics
//                            match issueRatings with
//                            | Meta.Answer issue ->
//                                saver.SaveIssueRatings issue
//                                let! issuerRatings = m.LoadMetadata<IssuerRatingData> rics
//                                match issuerRatings with
//                                | Meta.Answer issuer ->
//                                    saver.SaveIssuerRatings issuer
//                                    return Ok
//                                | Meta.Failed e -> return! loadFailed e
//                            | Meta.Failed e -> return! loadFailed e
//                        | Meta.Failed e -> return! loadFailed e
//                    | Meta.Failed e -> return! loadFailed e
//                with :? DbException as e -> 
//                    logger.ErrorEx "Load failed" e
//                    return! loadFailed e
//            }
   
        let rec reload (m:ChainMetaLoader) chains force = // todo chains are chain requests
            logger.Trace "reload ()"
            async {
                if force && saver.NeedsReload() || force then
                    try
                        saver.Backup ()
                        saver.Clear ()
                        return! load m chains
                    with :? DbException as e -> 
                        logger.ErrorEx "Load failed" e
                        return! loadFailed e
                else return Ok
            }

         and private load (m:ChainMetaLoader) requests = 
            logger.Trace "load ()"
            async {
                try
                    let! ricsByChain, fails = loadChains m requests
                    // todo throw fails
                    let rics = ricsByChain |> Array.map snd |> Array.collect id
                    
                    let! res = flow m rics
                    match res with 
                    | Some e -> return! loadFailed e
                    | None -> return Ok
                with :? DbException as e -> 
                    logger.ErrorEx "Load failed" e
                    return! loadFailed e
            }
        and private loadFailed (e:exn) = // todo e
            logger.Trace "loadFailed ()"
            async {
                try 
                    saver.Restore ()
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
    
    let load m c = Loading.reload m c |> asSuccess expectedLoadTime 
    let connect = Connecting.connect |> asSuccess expectedConnectTime

module AnotherStartup =
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

    type Severity = Note | Warn | Evil
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
        | Reload of State AsyncReplyChannel
        | Close of State AsyncReplyChannel
        override x.ToString () = 
            match x with
            | Connect _ -> "Connect"
            | Reload _ -> "Reload"
            | Close _ -> "Close"

    type Startup (f:EikonFactory, c:Calendar, m:ChainMetaLoader)  = 
        let n = Event<_> ()
        let s = Event<_> ()
        
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
                            n.Trigger <| (Started, f, Severity.Warn)
                            return! started (Some channel)
                        | Success.Ok -> return! connected channel
                    | Reload channel -> 
                        n.Trigger (Started, Problem <| sprintf "Invalid command %s in state Started" (cmd.ToString()), Severity.Warn)
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
                        n.Trigger (Connected, Problem <| sprintf "Invalid command %s in state Connected" (cmd.ToString()), Severity.Warn)
                        return! connected channel 
                    | Reload channel -> 
                        logger.Debug "[Primary reload]"
                        let x = []                                                      // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        let! res = ExternalOperations.load m x true
                        match res with
                        | Success.Failure f -> 
                            n.Trigger <| (Connected, f, Severity.Warn)
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
                        n.Trigger (Initialized, Problem <| sprintf "Invalid command %s in state Initialized" (cmd.ToString()), Severity.Warn)
                        return! initialized channel 
                    | Reload channel ->
                        logger.Debug "[Secondary reload]"
                        let x = []                                                      // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        let! res = ExternalOperations.load m x true
                        match res with
                        | Success.Failure f -> 
                            n.Trigger <| (Connected, f, Severity.Warn)
                            return! connected channel
                        | Success.Ok ->  return! initialized channel
                    | Close channel -> return close channel
                }

            and failed e = 
                logger.Debug "[--> failed ()]"
                n.Trigger (Closed, Error e, Severity.Evil)
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

        member x.Notification = n.Publish
        member x.StateChanged = s.Publish
        
        member x.Connect () = tryCommand Commands.Connect (ExternalOperations.expectedConnectTime + timeouts.agent)
        member x.Reload () = tryCommand Commands.Reload (ExternalOperations.expectedLoadTime + timeouts.agent)
        member x.Close () = tryCommand Commands.Close timeouts.agent