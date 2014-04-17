namespace YieldMap.Core.Application

[<AutoOpen>]
module Responses =
    type private FailureStatic = Failure
    and Failure = 
        | Problem of string 
        | Error of exn 
        | Timeout
        static member toString x = 
            match x with
            | Problem str -> sprintf "Problem %s" str
            | Error e -> sprintf "Error %s" (e.ToString())
            | Timeout -> "Timeout"
        override x.ToString() = FailureStatic.toString x

    type Success = 
        | Ok 
        | Failure of Failure
        override x.ToString() = 
            match x with
            | Ok -> "Ok"
            | Failure x -> sprintf "Failure %s" <| x.ToString()

[<AutoOpen>]
module internal Timeouts =
    type Timeouts = {
        load : int
        connect : int
        agent : int
        awaiter : int
    }

    let timeouts = { load = 5000; connect = 2000; agent = 1000; awaiter = 100 }

[<RequireQualifiedAccess>]
module private ExternalOperations =
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
            let! res = f.Connect () |> Async.WithTimeout (Some 10000) // todo default timeout
            return
                match res with
                | TimedOut -> Success.Failure Failure.Timeout
                | Failed e -> Success.Failure <| Failure.Error e
                | Established -> Success.Ok
        }

    [<RequireQualifiedAccess>]
    module private Loading = 
        open YieldMap.Tools.Aux.Workflows.Attempt

        type private LoadSteps = LoadBonds | LoadIssueRatings | LoadIssuerRatings | LoadFrns

        let private steps = [ LoadBonds; LoadIssueRatings; LoadIssuerRatings; LoadFrns ]
            
        let private clearDatabase () = attempt { 
            return () 
        }

        let private loadBonds () = attempt { 
            return () 
        }

        let private loadIssueRatings () = attempt { 
            return () 
        }

        let private loadIssuerRatings () = attempt { 
            return () 
        }

        let private loadFrns () = attempt { 
            return () 
        }

        let private load step = attempt {
            match step with
            | LoadBonds -> do! loadBonds ()
            | LoadIssueRatings -> do! loadIssueRatings ()
            | LoadIssuerRatings -> do! loadIssuerRatings ()
            | LoadFrns -> do! loadFrns ()
        }

        let reload force = async {
            logger.Trace "reload"
            // todo check last update date. do not force reload
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
            return 
                match res |> Attempt.runAttempt with
                | None ->  Success.Failure <| Problem "failed to load data" 
                | _ -> Success.Ok

            // todo catch EF errors too
            // todo create special kind of exception - ImportException - and catch it too
        }


    let load f = Loading.reload f
    let connect f = Connecting.connect f

    // todo more advanced evaluation
    let expectedLoadTime = timeouts.load
    let expectedConnectTime = timeouts.connect

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
                        let! res = ExternalOperations.load true
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
                        let! res = ExternalOperations.load true
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