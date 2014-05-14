namespace YieldMap.Core.Application

//[<AutoOpen>]
//module Timeouts =
//    type Timeouts = {
//        load : int
//        connect : int
//        agent : int
//        awaiter : int
//    }


module Startup =
    open YieldMap.Core.Application.Operations
    open YieldMap.Core.Notifier
    open YieldMap.Core.Portfolio
    
    open YieldMap.Database.StoredProcedures

    open YieldMap.Loader
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    
    open YieldMap.Requests
    open YieldMap.Requests.Responses
    
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

        let f = q.Factory
        let c = q.Calendar
        let m = q.Loader
        let dt = c.Today

        let reload = LoadAndSave q :> Operation<_,_>
        let connect = EstablishConnection f :> Operation<_,_>
        let shutdown = Shutdown () :> Operation<_,_>

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

            and doClose t channel = 
                async {
                    let! res = Operation.execute shutdown t
                    // TODO PARSE
                    return close channel
                }

            and doReload t force channel = 
                async {
                    try
                        use refresh = new Refresh()

                        let chainRequests = 
                            refresh.ChainsInNeed c.Today
                            |> Array.map (fun r -> { Ric = r.Name; Feed = r.Feed.Name; Mode = r.Params; Timeout = t}) 

                        let! res = reload.Execute ({Chains = chainRequests; Force = force}, Some t)
                        match res with
                        | Tweet.Failure f -> 
                            Notifier.notify ("Startup", f, Severity.Warn)
                            return! connected channel
                        | Tweet.Answer _ -> return! initialized channel
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

        member x.StateChanged = s.Publish

        member x.Connect (?t : int) = 
            let timeout = Operation.estimate connect t
            tryCommand (fun channel -> Commands.Connect (timeout, channel)) (timeout + addon)

        member x.Reload (force, ?t : int) = 
            let timeout = Operation.estimate reload t
            tryCommand (fun channel -> Commands.Reload (timeout, force, channel)) (timeout + addon)
        
        member x.Close (?t : int) = 
            let timeout = Operation.estimate shutdown t
            tryCommand (fun channel -> Commands.Close (timeout, channel)) (timeout + addon)