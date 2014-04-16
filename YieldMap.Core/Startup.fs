namespace YieldMap.Core.Application

[<AutoOpen>]
module Startup =
    open YieldMap.Loader
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.Requests

    open YieldMap.Tools.Logging
    open YieldMap.Tools.Aux

    open System

    let private logger = LogFactory.create "Startup"

    // Notifications
    type Failure = Problem of string | Error of exn
    type Success = Ok | Failure of Failure
    type Severity = Note | Warn | Evil

    [<StructuralEquality; StructuralComparison>]
    type AppState = 
        | Started 
        | Connected 
        | Initialized
        | Closed 
        override x.ToString () = 
            match x with
            | Started -> "Started" 
            | Connected -> "Connected"
            | Initialized -> "Initialized"
            | Closed -> "Closed"

    module private Loading = 
        open YieldMap.Tools.Aux.Workflows.Attempt

        type private LoadSteps = LoadBonds | LoadIssueRatings | LoadIssuerRatings | LoadFrns

        type Steps() =
            static let steps = [ LoadBonds; LoadIssueRatings; LoadIssuerRatings; LoadFrns ]
            
            static let clearDatabase () = attempt { 
                return () 
            }

            static let loadBonds () = attempt { 
                return () 
            }

            static let loadIssueRatings () = attempt { 
                return () 
            }

            static let loadIssuerRatings () = attempt { 
                return () 
            }

            static let loadFrns () = attempt { 
                return () 
            }

            static let load step = attempt {
                match step with
                | LoadBonds -> do! loadBonds ()
                | LoadIssueRatings -> do! loadIssueRatings ()
                | LoadIssuerRatings -> do! loadIssuerRatings ()
                | LoadFrns -> do! loadFrns ()
            }

            static member reload force = async {
                logger.Trace "reload"
                // todo check last update date. do not force relo
                let rec nextStep steps = attempt {
                    match steps with
                    | step :: rest -> 
                        do! load step
                        return! nextStep rest
                    | [] -> return ()
                }
                let res = attempt {
                    do! clearDatabase ()
                    do! nextStep steps
                }
                let res = res |> Attempt.runAttempt
                return match res with None ->  Some <| Problem "failed to load data" | _ -> None
                // todo catch EF errors too
                // todo create special kind of exception - ImportException - and catch it too
            }

    open Loading
   
    type StateCommands = 
        | Connect of AppState AsyncReplyChannel
        | Reload of AppState AsyncReplyChannel
        | Close of AppState AsyncReplyChannel
        | NotifyDisconnected of AppState AsyncReplyChannel
        static member channel = function
            | Connect channel -> channel
            | Reload channel -> channel
            | Close channel -> channel
            | NotifyDisconnected channel -> channel
        override x.ToString () = 
            match x with
                | Connect _ -> "Connect"
                | Reload _ -> "Reload"
                | Close _ -> "Close"
                | NotifyDisconnected _ -> "NotifyDisconnected"


    let (|TimedOut|Established|Failed|) = function
        | Some response ->
            match response with
            | Connection.Connected -> Established
            | Connection.Failed e -> Failed e
        | None -> TimedOut

    /// Responsibilities: 
    ///  - connect / disconnect; 
    ///  - database handling;
    ///  - handling of Tomorrow event
    type Startup (f:EikonFactory, c:Calendar, m:ChainMetaLoader) as self =

        let stateChanged = Event<_>()
        let notification = Event<_>()

        // todo it is easy to implement some async `Verifier` which will send a message to ask user if he or she would like to reload data
        do c.NewDay |> Observable.add (fun _ -> self.Reload() |> Async.Ignore |> Async.Start)
   
        let startupAgent = Agent.Start(fun inbox -> 
            let rec started (channel : AppState AsyncReplyChannel Option) = 
                async {
                    match channel with 
                    | Some c -> c.Reply Started 
                    | _ -> ()

                    logger.Trace "started"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    match command with
                    | Connect _ -> 
                        let! res = f.Connect () |> Async.WithTimeout (Some 10000) // todo default timeout
                        match res with
                        | TimedOut -> 
                            notification.Trigger <| (Problem "Connection timed out", Severity.Warn)
                            return! started (Some channel)
                        | Failed e ->
                            notification.Trigger <| (Error e, Severity.Warn)
                            return! started (Some channel)
                        | Established -> return! connected channel
                    | Close _ -> return close channel
                    | _ -> 
                        do warn command Started
                        return! started (Some channel)
               } 
            and connected (channel : AppState AsyncReplyChannel) = 
                async {
                    channel.Reply Connected
                    logger.Trace "connected"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    match command with
                    | Close _ -> return close channel
                    | Reload _ -> 
                        let! res = Steps.reload false
                        match res with // todo return! initializing Steps.reload
                        | None ->  return! initialized channel
                        | Some failure ->
                            notification.Trigger <| (failure, Severity.Warn)
                            return! connected channel
                    | NotifyDisconnected _ ->
                        notification.Trigger <| (Problem "Disconnected", Severity.Warn)
                        return! started (Some channel)
                    | _ -> 
                        do warn command Connected
                        return! connected channel
                }            
            and initialized (channel : AppState AsyncReplyChannel) =
                async {
                    channel.Reply Initialized
                    logger.Trace "initialized"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    match command with
                    | Close _ -> return close channel
                    | Reload _ -> 
                        let! res = Steps.reload true
                        match res with // todo return! initializing Steps.reload
                        | None ->  return! initialized channel
                        | Some failure ->
                            notification.Trigger <| (failure, Severity.Warn)
                            return! connected channel
                    | Connect _ -> 
                        // todo notify that ok
                        return! initialized channel
                    | NotifyDisconnected _ -> 
                        // todo notify that disconnected
                        return! started (Some channel)
                }
            and close channel = 
                logger.Trace "close"
                channel.Reply Closed
                stateChanged.Trigger Closed
            and warn command state = 
                logger.Trace "warn"
                let msg = sprintf "Invalid command %s in state %A" (command.ToString()) state
                notification.Trigger <| (Problem msg, Severity.Note)
                logger.Warn msg                

            started None
        )

        member x.StateChanged = stateChanged.Publish
        member x.Notification = notification.Publish

        member x.Initialze () = 
            startupAgent.PostAndReply Connect
        member x.Reload () = 
            startupAgent.PostAndAsyncReply Reload
        member x.Shutdown () = 
            startupAgent.PostAndAsyncReply Close

module AnotherStartup =
    open System.Threading
    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "Startup"

    type State =
        | Started 
        | Connected
        | Initialized
        | Closed

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

    type Timeouts = {
        load : int
        connect : int
        agent : int
        awaiter : int
    }

    let timeouts = { load = 5000; connect = 2000; agent = 1000; awaiter = 100 }
    
    let doLoad () = async {
        logger.Info "doLoad() start"
        do! Async.Sleep timeouts.load
        logger.Info "doLoad() finish"
        return ()
    }

    let doConnect () = async {
        logger.Info "doConnect() start"
        do! Async.Sleep timeouts.connect
        logger.Info "doConnect() finish"
        return ()
    }

    type Boxing() = 
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
                        do! doConnect ()
                        return! connected channel // todo failure check
                    | Reload channel -> 
                        n.Trigger (Started, sprintf "Invalid command %s in state Started" (cmd.ToString()))
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
                        n.Trigger (Connected, sprintf "Invalid command %s in state Connected" (cmd.ToString()))
                        return! connected channel 
                    | Reload channel -> 
                        logger.Debug "[Primary reload]"
                        do! doLoad () // todo primary / secondary and failure check
                        return! initialized channel
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
                            n.Trigger (Initialized, sprintf "Invalid command %s in state Initialized" (cmd.ToString()))
                            return! initialized channel 
                        | Reload channel ->
                            logger.Debug "[Secondary reload]"
                            do! doLoad () // todo primary / secondary and failure check
                            return! initialized channel
                        | Close channel -> return close channel
                }

            and close channel = 
                logger.Debug "[--> closed ()]"
                s.Trigger Closed
                channel.Reply Closed

            started None 
        )

        let tryCommand command timeout = async {
            let! answer = a.PostAndTryAsyncReply (command, timeout)
            match answer with
            | Some state -> return State state
            | None -> return NotResponding
        }        

        member x.Notification = n.Publish
        member x.StateChanged = n.Publish
        
        member x.Connect = tryCommand Commands.Connect (timeouts.connect + timeouts.agent)
        member x.Reload = tryCommand Commands.Reload (timeouts.load + timeouts.agent)
        member x.Close = tryCommand Commands.Close timeouts.agent