namespace YieldMap.Core.Application

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

    let logger = LogFactory.create "Startup"

    [<StructuralComparison; StructuralEquality>] 
    type AppState = 
        | Started 
        | Connected 
        | Loaded of int
        | Initialized
        | Disconnected 
        | Closed 

    type Success = 
        | Ok 
        | Problem of string 
        | Error of exn

    module private Loading = 
        type private LoadSteps = LoadBonds | LoadIssueRatings | LoadIssuerRatings | LoadFrns

        type Steps() =
            static let steps = [| LoadBonds; LoadIssueRatings; LoadIssuerRatings; LoadFrns |]
        
            static member count = (Array.length steps)-1 

            static member earlier step = 
                let minStep = max 0 (step-1)
                let maxStep = min minStep Steps.count
                Connected :: [ for i in 0..maxStep -> Loaded i ]

            static member load step = 
                // Todo dodo
                try
                    let answer = 
                        match steps.[step] with
                        | LoadBonds -> Success.Ok
                        | LoadIssueRatings -> Success.Ok
                        | LoadIssuerRatings -> Success.Ok
                        | LoadFrns -> Success.Ok
                    answer
                with :? IndexOutOfRangeException as e -> Success.Error e

    open Loading
   
    type StateCommands = 
        | Connect of AppState AsyncReplyChannel
        | Load of int * AppState AsyncReplyChannel
        | Close of AppState AsyncReplyChannel
        | NotifyDisconnected of AppState AsyncReplyChannel
        static member channel = function
            | Connect channel -> channel
            | Load (_, channel) -> channel
            | Close channel -> channel
            | NotifyDisconnected channel -> channel

    type StateChangeFeedback = AppState * Success

    let (|TimedOut|Established|Failed|) = function
        | Some response ->
            match response with
            | Connection.Connected -> Established
            | Connection.Failed e -> Failed e
        | None -> TimedOut

    let (|DoConnect|_|) state = function
        | Connect channel when state |- [Started; Disconnected] -> Some ()
        | _ -> None

    let (|LoadStep|_|) state = function
        | Load (step, channel) when state |- (Steps.earlier step) && step <= Steps.count -> Some step
        | _ -> None

    let (|LastStep|_|) state = function
        | Load (step, channel) when state = Loaded Steps.count -> Some ()
        | _ -> None

    /// Responsibilities: 
    ///  - connect / disconnect; 
    ///  - database handling;
    ///  - handling of Tomorrow event
    type Startup (f:EikonFactory, c:Calendar, m:ChainMetaLoader) as self =

        let stateChanged = Event<_>()
        let notification = Event<_>()

        do c.NewDay |> Observable.add (fun dt -> self.Reload ())
   
        let startupAgent = Agent.Start(fun inbox -> // maybe I should make loadingtools a param ???
            let rec loop state = async {
                let! command = inbox.Receive ()
                let channel = StateCommands.channel command
                let newState = 
                    match command with
                    | DoConnect state -> 
                        let res = f.Connect () |> Async.WithTimeout (Some 10000) |> Async.RunSynchronously // todo timeout
                        match res with
                        | TimedOut -> 
                            notification.Trigger (state, Problem "Connection timed out")
                            Disconnected
                        | Failed e ->
                            notification.Trigger (state, Error e)
                            Disconnected
                        | Established -> Connected

                    | LoadStep state step -> 
                        // perform Loading step!
                        match Steps.load step with
                        | Ok -> Loaded step
                        | failure ->
                            notification.Trigger (state, failure)
                            if step = 0 then Connected else Loaded (step-1) // todo ???

                    | LastStep state ->
                        let step = Steps.count
                        match Steps.load step with
                        | Ok ->  Initialized 
                        | failure ->
                            notification.Trigger (state, failure)
                            if step = 0 then Connected else Loaded (step-1) // todo ???

                    | Close _ -> 
                        // todo teardown
                        Closed

                    | NotifyDisconnected _ -> 
                        // todo stop current operations, do necessary cleanups and so on
                        Disconnected 

                    | _ -> // In any other case just stay in current state
                        logger.WarnF "Invalid state %A for command %A" state command
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

        member x.Initialze () = 
            ()

        member x.Reload () =
            ()

        member x.Shutdown () =
            ()