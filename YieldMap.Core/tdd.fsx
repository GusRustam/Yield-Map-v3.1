#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Core\bin\debug\YieldMap.Core.dll"
#endif

module ``Working with mailbox`` =
    open System.Threading
    open YieldMap.Tools.Aux

    type State =
        | Started 
        | Connected
        | Initialized
        | Closed

    type Commands = 
        | Connect of State AsyncReplyChannel
        | Reload of State AsyncReplyChannel
        | Close of State AsyncReplyChannel
        override x.ToString () = 
            match x with
            | Connect _ -> "Connect"
            | Reload _ -> "Reload"
            | Close _ -> "Close"

    let longDatabaseInitialization () = async {
        do! Async.Sleep 5000
        return ()
    }

    type Boxing() = 
        let n = Event<_> ()
        let a = MailboxProcessor.Start (fun inbox ->

            let rec started (channel : State AsyncReplyChannel option) = 
                printfn "[--> started ()]"
                async {
                    match channel with Some che -> che.Reply Started | None -> ()

                    let! cmd = inbox.Receive ()
                    printfn "Started: message %s" (cmd.ToString())
                    match cmd with 
                    | Connect channel -> 
                        do! Async.Sleep 500
                        return! connected channel
                    | Reload channel -> 
                        n.Trigger (Started, sprintf "Invalid command %s in state Started" (cmd.ToString()))
                        return! started (Some channel)
                    | Close channel -> return close channel
                } 
            and connected channel = 
                printfn "[--> connected ()]"
                async {
                    channel.Reply Connected

                    let! cmd = inbox.Receive ()
                    printfn "Connected: message %s" (cmd.ToString())
                    match cmd with 
                    | Connect channel -> 
                        n.Trigger (Connected, sprintf "Invalid command %s in state Connected" (cmd.ToString()))
                        return! connected channel
                    | Reload channel -> 
                        printfn "[Primary reload]"
//                        do! longDatabaseInitialization ()
//                        return! initialized channel
                        return! initializing longDatabaseInitialization channel
                    | Close channel -> return close channel
                }
            and initialized (channel : State AsyncReplyChannel) = 
                printfn "[--> initialized ()]"
                async {
                    channel.Reply Initialized
                    let! cmd = inbox.Receive ()
                    printfn "Initialized: message %s" (cmd.ToString())
                    match cmd with 
                        | Connect channel ->
                            n.Trigger (Initialized, sprintf "Invalid command %s in state Initialized" (cmd.ToString()))
                            return! initialized channel
                        | Reload channel ->
                            printfn "[Secondary reload]"
                            return! initializing longDatabaseInitialization channel
//                            do! longDatabaseInitialization ()
//                            return! initialized channel
                        | Close channel -> return close channel
                }
            and initializing op channel = 
                printfn "[--> initializing ()]"
                let tokenSrc = new CancellationTokenSource ()

                let operation = async {
                    printfn "[--|--> operation () STARTED]"
                    let! res =  op () |> Async.WithCancellation tokenSrc.Token
                    printfn "[--|--> operation () FINISHED]"
                    match res with 
                    | Some () -> 
                        printfn "[--|--> operation () SUCCESS]"
                        return! initialized channel
                    | _ -> 
                        printfn "[--|--> operation () FAILED]"
                        return! connected channel
                }

                let rec awaiter () = async {
                    try
                        printfn "[--|--> awaiter ()]"
                        let! cmd = inbox.Receive ()
                        printfn "Awaiter: message %s" (cmd.ToString())
                        match cmd with 
                        | Connect channel | Reload channel -> 
                            n.Trigger (Connected, sprintf "Invalid command %s in state Awaiter" (cmd.ToString()))
                            channel.Reply Connected
                            return! awaiter ()
                        | Close channel -> 
                            tokenSrc.Cancel ()
                            return close channel
                    with e -> printfn "%s" (e.ToString())
                }

                let awt = awaiter () |> Async.WithCancellation tokenSrc.Token |> Async.Ignore

                Async.Parallel [ operation; awt] |> Async.Ignore
                
            and close channel = 
                printfn "[--> closed ()]"
                channel.Reply Closed

            started None
        )

        member x.N = n.Publish
        member x.RunCommand p = a.PostAndAsyncReply p
        member x.RunCommand (p, t) = a.PostAndTryAsyncReply (p, t)


    let flow id (x : Boxing) = 
        async {
            x.N |> Observable.add (fun (state, msg) -> printfn "[%A: %d -> %s]" state id msg)
            
            printfn "[%d sending Connect]" id 
            let! res = x.RunCommand Commands.Connect
            printfn "[%d res in %A]" id res

            printfn "[%d sending Reload]" id 
            let! res = x.RunCommand Commands.Reload
            printfn "[%d res in %A]" id res

            printfn "[%d sending Close]" id 
            let! res = x.RunCommand Commands.Close
            printfn "[%d res in %A]" id res
    }

    let flowT id (x : Boxing) T = 
        async {
            x.N |> Observable.add (fun (state, msg) -> printfn "[%A: %d -> %s]" state id msg)
            
            printfn "[%d ====> Connect]" id 
            let! res = x.RunCommand (Commands.Connect, T)
            printfn "[%d res in %A]" id res

            printfn "[%d ====> Reload]" id 
            let! res = x.RunCommand (Commands.Reload, T)
            printfn "[%d res in %A]" id res

            printfn "[%d ====> Close]" id 
            let! res = x.RunCommand (Commands.Close, T)
            printfn "[%d res in %A]" id res
        }

    let someTest () = 
        let x = Boxing ()
        flow 1 x |> Async.RunSynchronously

    let anotherTest () =
        let x = Boxing ()
        
        flow 1 x |> Async.Start
        Thread.Sleep 100
        flow 2 x |> Async.Start
        Thread.Sleep 15000

    let thirdTest () =
        let x = Boxing ()
       
        let pFlow timeout id x = async {
            do! Async.Sleep timeout
            return! flow id x
        }

        Async.Parallel [flow 1 x; pFlow 1000 2 x] |> Async.RunSynchronously

    let fourthTest () =
        let x = Boxing ()
       
        let pFlowT pause id x T = async {
            do! Async.Sleep pause
            return! flowT id x T
        }
        let timeout = 1000
        let pause = 1000
        Async.Parallel [flowT 1 x timeout; pFlowT pause 2 x timeout] |> Async.RunSynchronously |> ignore
        printfn "[FINISHED]"
        

open ``Working with mailbox``
fourthTest ()