namespace YieldMap.Core.Application

module Startup =
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.Requests
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Aux

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
                match steps.[step] with
                | LoadBonds -> ()
                | LoadIssueRatings -> ()
                | LoadIssuerRatings -> ()
                | LoadFrns -> ()
                Ok

    open Loading
   
    type StateCommands = 
    | Connect 
    | Load of int 
    | Close 
    | NotifyDisconnected

    type StateChangeFeedback = AppState * Success

    let (|TimedOut|Established|Failed|) = function
        | Some response ->
            match response with
            | Connection.Connected -> Established
            | Connection.Failed e -> Failed e
        | None -> TimedOut

    let (|DoConnect|_|) state = function
        | Connect when state |- [Started; Disconnected] -> Some ()
        | _ -> None

    let (|LoadStep|_|) state = function
        | Load step when state |- (Steps.earlier step) && step <= Steps.count -> Some step
        | _ -> None

    let (|LastStep|_|) state = function
        | Load step when state = Loaded Steps.count -> Some ()
        | _ -> None

    /// Responsibilities: 
    ///  - connect / disconnect; 
    ///  - database handling;
    ///  - handling of Tomorrow event
    type Startup(f:EikonFactory) =
        let startupAgent (replyChanned : _ AsyncReplyChannel) = Agent.Start(fun inbox ->
            let rec loop state = async {
                replyChanned.Reply (state, Ok)
                let! command = inbox.Receive ()
                match command with
                | DoConnect state -> 
                    let res = f.Connect () |> Async.WithTimeout (Some 10000) |> Async.RunSynchronously 
                    match res with
                    | TimedOut -> 
                        replyChanned.Reply (state, Problem "Connection timed out")
                        return! loop Disconnected
                    | Failed e ->
                        replyChanned.Reply (state, Error e)
                        return! loop Disconnected
                    | Established -> return! loop Connected

                | LoadStep state step -> 
                    // perform Loading step!
                    match Steps.load step with
                    | Ok -> return! loop (Loaded step)
                    | failure ->
                        replyChanned.Reply (state, failure)
                        return! loop (if step = 0 then Connected else Loaded (step-1)) // todo ???

                | LastStep state ->
                    let step = Steps.count
                    match Steps.load step with
                    | Ok ->  return! loop Initialized 
                    | failure ->
                        replyChanned.Reply (state, failure)
                        return! loop (if step = 0 then Connected else Loaded (step-1)) // todo ???

                | Close -> 
                    // todo teardown
                    return ()

                | NotifyDisconnected -> 
                    // todo stop current operations, do necessary cleanups and so on
                    return! loop Disconnected 

                | _ -> // In any other case just stay in current state
                    logger.WarnF "Invalid state %A for command %A" state command
                    return! loop state 
            }
            loop Started
        )

        // todo operations and so on. Events, you know... =)