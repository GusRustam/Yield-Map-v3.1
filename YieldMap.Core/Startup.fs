namespace YieldMap.Core

module Startup =
    open Autofac

    open Operations
    open Notifier
    open DbManager

    open YieldMap.Database

    open YieldMap.Loader
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    
    open YieldMap.Requests
    
    open YieldMap.Tools.Aux
    open YieldMap.Tools.Response
    open YieldMap.Tools.Logging

    open YieldMap.Transitive.Procedures

    open System
    open System.Linq
    open System.Threading
    open YieldMap.Transitive.Registry
    open YieldMap.Transitive.Domains.NativeContext

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
        | Connect of int * State AsyncReplyChannel
        | Reload of int * bool * State AsyncReplyChannel
        | Close of int * State AsyncReplyChannel
        override x.ToString () = 
            match x with
            | Connect _ -> "Connect"
            | Reload _ -> "Reload"
            | Close _ -> "Close"

    type Startup (q:Drivers) = 
        let s = Event<_> ()

        let c = q.Calendar

        let reload = LoadAndSave q :> Operation<_,_>
        let connect = EstablishConnection q.Factory :> Operation<_,_>
        let shutdown = Shutdown () :> Operation<_,_>

        let mutable shut = false

        let a = MailboxProcessor.Start (fun inbox ->
            let rec started (channel : State AsyncReplyChannel option) = 
                logger.Debug "[--> started ()]"

                async {
                    s.Trigger Started
                    match channel with Some che -> che.Reply Started | None -> ()

                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Started: message %s]" (cmd.ToString())
                    match cmd with
                    | Connect (t, channel) -> 
                        let! res = Operation.execute connect t
                        match res with
                        | Tweet.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! started (Some channel)
                        | Tweet.Answer _ -> return! connected channel
                    | Reload (_, _, channel) -> 
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Started" (cmd.ToString()), Severity.Warn)
                        return! started (Some channel) 
                    | Close (t, channel) -> return! doClose t channel
                }

            and connected (channel : State AsyncReplyChannel) = 
                logger.Debug "[--> connected ()]"

                async {
                    s.Trigger Connected
                    channel.Reply Connected

                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Connected: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect (_, channel) -> 
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Connected" (cmd.ToString()), Severity.Warn)
                        return! connected channel 
                    | Reload (t, _, channel) -> // ignoring force parameter on primary loading
                        logger.Debug "[Primary reload]"
                        return! doReload t true channel
                    | Close (t, channel) -> return! doClose t channel

                }

            and initialized (channel : State AsyncReplyChannel) = 
                logger.Debug "[--> initialized ()]"

                async {
                    s.Trigger Initialized
                    channel.Reply Initialized
                    let! cmd = inbox.Receive ()
                    logger.DebugF "[Initialized: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect (_, channel) ->
                        Notifier.notify ("Startup", Problem <| sprintf "Invalid command %s in state Initialized" (cmd.ToString()), Severity.Warn)
                        return! initialized channel 
                    | Reload (t, force, channel) ->
                        logger.Debug "[Secondary reload]"
                        return! doReload t force channel
                    | Close (t, channel) -> return! doClose t channel
                }

            and failed e = 
                logger.Debug "[--> failed ()]"
                Notifier.notify ("Startup", Error e, Severity.Evil)
                s.Trigger Closed

            and close (channel : _ AsyncReplyChannel) = 
                logger.Debug "[--> closed ()]"
                s.Trigger Closed
                channel.Reply Closed
                shut <- true

            and doClose t channel = 
                async {
                    let! res = Operation.execute shutdown t
                    match res with
                    | Tweet.Failure f ->
                        Notifier.notify ("Close", f, Severity.Warn)
                    | _ -> ()

                    return close channel
                }

            and doReload t force channel = 
                let updater = q.DbServices.Resolve<IDbUpdates>()
                if force then
                    let registry = q.DbServices.Resolve<IFunctionRegistry>()
                    use properties = q.DbServices.Resolve<INPropertiesReader>()
                    registry.Clear () |> ignore
                    properties
                        .FindAll()
                        .ToList() 
                        |> Seq.iter(fun p -> registry.Add(p.id, p.Expression) |> ignore)

                async {
                    try
                        let chainRequests = 
                            c.Today
                            |> updater.ChainsInNeed 
                            |> Seq.map (fun r -> { Ric = r.Name; Feed = r.Feed.Name; Mode = r.Params; Timeout = t}) 
                            |> Array.ofSeq

                        let! res = reload.Execute ({Chains = chainRequests; Force = force}, Some t)
                        match res with
                        | Tweet.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! connected channel

                        | Tweet.Answer _ -> 
                            return! initialized channel

                    with e ->
                        logger.ErrorEx "Primary reload failed" e
                        Notifier.notify ("Startup", Error e, Severity.Warn)
                        return! connected channel
                }

            // MAIN
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

        let addon = 1000 // additional agent's wait time
        
        // if server is down, set standard feedback timeout, otherwise use given timeout
        let shutdownTimeout t = if shut then Some addon else t 

        member __.StateChanged = s.Publish

        member __.Connect (?t : int) = 
            let timeout = shutdownTimeout t |> Operation.estimate connect
            logger.TraceF "Connect timeout is %d" timeout
            tryCommand (fun channel -> Commands.Connect (timeout, channel)) (timeout + addon)

        member __.Reload (force, ?t : int) = 
            let timeout = shutdownTimeout t |> Operation.estimate reload
            logger.TraceF "Reload timeout is %d" timeout
            tryCommand (fun channel -> Commands.Reload (timeout, force, channel)) (timeout + addon)
        
        member __.Close (?t : int) = 
            let timeout = shutdownTimeout t |> Operation.estimate shutdown
            logger.TraceF "Close timeout is %d" timeout
            tryCommand (fun channel -> Commands.Close (timeout, channel)) (timeout + addon)