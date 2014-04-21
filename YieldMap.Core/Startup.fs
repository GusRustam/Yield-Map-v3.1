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
        open YieldMap.Tools.Location
        open YieldMap.Tools.Logging
        open YieldMap.Tools.Aux.Workflows.Attempt

        open System.IO

        let private logger = LogFactory.create "Loading"

        do MainEntities.SetVariable("PathToTheDatabase", Location.path)
        let private cnnStr = MainEntities.GetConnectionString("TheMainEntities")

        exception DbException of Failure

        type LoadingOperations =
            abstract LoadChains : string list -> string list // chains to rics, todo some params if any
            abstract LoadBonds : string list -> unit // todo some metatable
            abstract LoadRatings : string list -> unit // todo some metatable
            abstract LoadFrns : string list -> unit // todo some metatable
            abstract LoadRics : string list -> unit // todo some metatable // todo use it externally

        // TODO IMPLEMENTS
        type EikonLoadingOperations (m:ChainMetaLoader) =
            interface LoadingOperations with
                member x.LoadChains chains = []
                member x.LoadBonds rics = ()
                member x.LoadRatings rics = ()
                member x.LoadFrns rics = ()
                member x.LoadRics rics = ()

        type SavingOperations = 
            abstract Backup : unit -> unit
            abstract Restore : unit -> unit
            abstract Clear : unit -> unit
            abstract NeedsReload : unit -> bool
            abstract SaveBonds : unit -> unit
            abstract SaveRatings : unit -> unit
            abstract SaveFrns : unit -> unit

        type DbSavingOperations () = 
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

        let private saver = DbSavingOperations () :> SavingOperations
        let private loader = EikonLoadingOperations () :> LoadingOperations

        let rec reload (m:ChainMetaLoader) force = 
            logger.Trace "reload ()"
            async {
                if force && reloadNecessary () || force then
                    try
                        saver.Backup ()
                        saver.Clear ()
                        return! load ()
                    with :? DbException as e -> 
                        logger.ErrorEx "Load failed" e
                        return! loadFailed ()
                else return Ok
            }

        and private load () = 
            logger.Trace "load ()"
            async {
                try
                    saver.SaveBonds ()
                    saver.SaveFrns ()
                    saver.SaveRatings ()
                    return Ok
                with :? DbException as e -> 
                    logger.ErrorEx "Load failed" e
                    return! loadFailed ()
            }
        and private reloadNecessary () = 
            logger.Trace "reloadNecessary ()"
            false
        and private loadFailed () = 
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
    
    let load m = Loading.reload m |> asSuccess expectedLoadTime 
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
                        let! res = ExternalOperations.load m true
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
                        let! res = ExternalOperations.load m true
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