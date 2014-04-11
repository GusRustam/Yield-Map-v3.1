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

    [<StructuralComparison; StructuralEquality>] 
    type AppState = 
        | Started 
        | Connected 
        | Loaded of int
        | Initialized
        | Disconnected 
        | Closed 


    // Notifications
    type Failure = Problem of string | Error of exn
    type Severity = Note | Warn | Evil
    type StartupNotification = 
        | Connection of Failure
        | Database of Failure
        | DatabaseReloading
        | Message of Failure * Severity


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

            static member reload = 
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
                match res with None -> Some <| Problem "failed to load data" | _ -> None
                // todo catch EF errors too
                // todo create special kind of exception - ImportException - and catch it too

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

    let (|DoConnect|_|) state = function
        | Connect _ when state |- [Started; Disconnected] -> Some ()
        | _ -> None

    let (|Reload|_|) state = function
        | Reload _ when state |- [Connected; Initialized] -> Some ()
        | _ -> None

    /// Responsibilities: 
    ///  - connect / disconnect; 
    ///  - database handling;
    ///  - handling of Tomorrow event
    type Startup (f:EikonFactory, c:Calendar, m:ChainMetaLoader) as self =

        let stateChanged = Event<_>()
        let notification = Event<_>()

        do c.NewDay |> Observable.add (fun _ -> self.Reload() |> Async.Ignore |> Async.Start) // Мне это нинравицо. Фсе пачиму?
   
        let startupAgent = Agent.Start(fun inbox -> 
            let rec loop state = async {
                let! command = inbox.Receive ()
                let channel = StateCommands.channel command
                let newState = 
                    match command with
                    | DoConnect state -> 
                        let res = f.Connect () |> Async.WithTimeout (Some 10000) |> Async.RunSynchronously // todo default timeout
                        match res with
                        | TimedOut -> 
                            notification.Trigger <| Connection (Problem "Connection timed out")
                            Disconnected
                        | Failed e ->
                            notification.Trigger <| Connection (Error e)
                            Disconnected
                        | Established -> Connected

                    | Reload state ->
                        notification.Trigger DatabaseReloading
                        match Steps.reload with
                        | None ->  Initialized 
                        | Some failure ->
                            notification.Trigger <| Database failure
                            Connected

                    | Close _ -> 
                        // todo teardown
                        Closed

                    | NotifyDisconnected _ -> 
                        // todo stop current operations, do necessary cleanups and so on
                        Disconnected 

                    | _ -> // In any other case just stay in current state
                        let msg = sprintf "Invalid command %s in state %A" (command.ToString()) state
                        notification.Trigger <| Message (Problem msg, Note)
                        logger.Warn msg
                        state 

                channel.Reply newState          // answer directly
                stateChanged.Trigger newState   // notify people

                if newState <> Closed then
                    return! loop newState   
                else return ()
            }

            loop Started
        )

        member x.StateChanged = stateChanged.Publish
        member x.Notification = notification.Publish

        member x.Reload () = startupAgent.PostAndAsyncReply Reload
        member x.Initialze () = startupAgent.PostAndAsyncReply Connect
        member x.Shutdown () = startupAgent.PostAndAsyncReply Close