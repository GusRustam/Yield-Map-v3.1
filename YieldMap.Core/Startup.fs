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

        do c.NewDay |> Observable.add (fun _ -> self.Reload() |> Async.Ignore |> Async.Start)
   
        let startupAgent = Agent.Start(fun inbox -> 
            let rec started first = 
                async {
                    logger.Trace "started"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    if not first then channel.Reply Started 
                    match command with
                    | Connect _ -> 
                        let! res = f.Connect () |> Async.WithTimeout (Some 10000) // todo default timeout
                        match res with
                        | TimedOut -> 
                            notification.Trigger <| (Problem "Connection timed out", Severity.Warn)
                            return! started false
                        | Failed e ->
                            notification.Trigger <| (Error e, Severity.Warn)
                            return! started false
                        | Established -> return! connected ()
                    | Close _ -> return close channel
                    | _ -> 
                        do warn command Started
                        return! started false
               } 
            and connected () = 
                async {
                    logger.Trace "connected"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    channel.Reply Connected
                    match command with
                    | Close _ -> return close channel
                    | Reload _ -> 
                        let! res = Steps.reload false
                        match res with // todo return! initializing Steps.reload
                        | None ->  return! initialized () 
                        | Some failure ->
                            notification.Trigger <| (failure, Severity.Warn)
                            return! connected ()
                    | NotifyDisconnected _ ->
                        notification.Trigger <| (Problem "Disconnected", Severity.Warn)
                        return! started false
                    | _ -> 
                        do warn command Connected
                        return! connected ()
                }            
            and initialized () =
                async {
                    logger.Trace "initialized"
                    let! command = inbox.Receive ()
                    let channel = StateCommands.channel command
                    channel.Reply Initialized
                    match command with
                    | Close _ -> return close channel
                    | Reload _ -> 
                        let! res = Steps.reload true
                        match res with // todo return! initializing Steps.reload
                        | None ->  return! initialized () 
                        | Some failure ->
                            notification.Trigger <| (failure, Severity.Warn)
                            return! connected ()
                    | Connect _ -> 
                        // todo notify that ok
                        return! initialized ()
                    | NotifyDisconnected _ -> 
                        // todo notify that disconnected
                        return! started false
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


            started true
        )

        member x.StateChanged = stateChanged.Publish
        member x.Notification = notification.Publish

        member x.Initialze () = 
            startupAgent.PostAndTryAsyncReply (Connect, 10000)
        member x.Reload () = 
            startupAgent.PostAndTryAsyncReply (Reload, 10000)
        member x.Shutdown () = 
            startupAgent.PostAndTryAsyncReply (Close, 10000)

//
//    open System.Threading
//            and initializing operation = 
//                async {
//                    use token = new CancellationTokenSource ()
//
//                    //
//                    let rec eventQueue () = 
//                        async {
//                            try
//                                let! command = inbox.Receive ()
//                                let channel = StateCommands.channel command
//                                match command with
//                                | Close _  | NotifyDisconnected _ -> 
//                                    token.Cancel()
//                                    return Ok
//                                | Reload _ -> return! eventQueue ()
//                                | Connect _ -> return! eventQueue ()
//                            with :? ThreadInterruptedException -> 
//                                return Ok
//                        }
//
//                    let q = Async.StartAsTask(Async.Parallel [ eventQueue (); operation ], cancellationToken = token.Token)
//                    q.Wait()
//                    let res = q.Result
//                    let res = res.[1]
//                                        
//                    match res with
//                    | Ok -> return! initialized ()
//                    | Failure f -> 
//                        notification.Trigger <| (f, Severity.Warn)
//                        return! connected ()
//                }