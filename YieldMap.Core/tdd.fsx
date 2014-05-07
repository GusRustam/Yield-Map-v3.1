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
                    printfn "[Started: message %s]" (cmd.ToString())
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
                    printfn "[Connected: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect channel -> 
                        n.Trigger (Connected, sprintf "Invalid command %s in state Connected" (cmd.ToString()))
                        return! connected channel
                    | Reload channel -> 
                        printfn "[Primary reload]"
                        return! initializing longDatabaseInitialization channel
                    | Close channel -> return close channel
                }
            and initialized (channel : State AsyncReplyChannel) = 
                printfn "[--> initialized ()]"
                async {
                    channel.Reply Initialized
                    let! cmd = inbox.Receive ()
                    printfn "[Initialized: message %s]" (cmd.ToString())
                    match cmd with 
                        | Connect channel ->
                            n.Trigger (Initialized, sprintf "Invalid command %s in state Initialized" (cmd.ToString()))
                            return! initialized channel
                        | Reload channel ->
                            printfn "[Secondary reload]"
                            return! initializing longDatabaseInitialization channel
                        | Close channel -> return close channel
                }
            and initializing op channel = 
                printfn "[--> initializing ()]"
                let tokenSrc = new CancellationTokenSource ()

                let closeRequested = ref false
                let operationFinished = ref false


                let operation = async {
                    printfn "[--|--> operation () STARTED]"
                    let! res =  op () |> Async.WithCancellation tokenSrc.Token
                    printfn "[--|--> operation () FINISHED]"
                    operationFinished := true
                    match res with 
                    | Some () -> 
                        printfn "[--|--> operation () SUCCESS]"
                        return! initialized channel
                    | _ -> 
                        printfn "[--|--> operation () FAILED]"
                        if !closeRequested then
                            return close channel
                        else return! connected channel
                }

                let rec awaiter () = async {
                    if !operationFinished then return ()

                    printfn "[--|--> awaiter ()]"
                    let! cmd = inbox.Receive ()
                    printfn "[Awaiter: message %s]" (cmd.ToString())
                    match cmd with 
                    | Connect channel | Reload channel -> 
                        n.Trigger (Connected, sprintf "Invalid command %s in state Awaiter" (cmd.ToString()))
                        channel.Reply Connected
                        return! awaiter ()
                    | Close channel -> 
                        closeRequested := true
                        tokenSrc.Cancel ()
                        return close channel
                }

                let awt = awaiter () |> Async.WithCancellation tokenSrc.Token |> Async.Ignore

                Async.Parallel [ operation; awt] |> Async.Ignore
                
            and close channel = 
                printfn "[--> closed ()]"
                channel.Reply Closed

            started None
        )

        member x.N = n.Publish
        member x.RunCommandSync p = a.PostAndReply p
        member x.RunCommand p = a.PostAndAsyncReply p
        member x.RunCommand (p, t) = a.PostAndTryAsyncReply (p, t)


    let flow id (x : Boxing) = 
        async {
            x.N |> Observable.add (fun (state, msg) -> printfn "[%A: %d -> %s]" state id msg)
            
            printfn "[%d sending Connect]" id 
            let! res = x.RunCommand Commands.Connect
            printfn "[%d <==== %A]" id res

            printfn "[%d sending Reload]" id 
            let! res = x.RunCommand Commands.Reload
            printfn "[%d <==== %A]" id res

            printfn "[%d sending Close]" id 
            let! res = x.RunCommand Commands.Close
            printfn "[%d <==== %A]" id res
    }

    let flowSync id (x : Boxing) = async {
        x.N |> Observable.add (fun (state, msg) -> printfn "[%A: %d -> %s]" state id msg)
            
        printfn "[%d sending Connect]" id 
        let res = x.RunCommandSync Commands.Connect
        printfn "[%d <==== %A]" id res

        printfn "[%d sending Reload]" id 
        let res = x.RunCommandSync Commands.Reload
        printfn "[%d <==== %A]" id res

        printfn "[%d sending Close]" id 
        let res = x.RunCommandSync Commands.Close
        printfn "[%d <==== %A]" id res
    }

    let flowT id (x : Boxing) T = 
        async {
            x.N |> Observable.add (fun (state, msg) -> printfn "[%A: %d -> %s]" state id msg)
            
            printfn "[%d ====> Connect]" id 
            let! res = x.RunCommand (Commands.Connect, T)
            printfn "[%d <==== %A]" id res

            printfn "[%d ====> Reload]" id 
            let! res = x.RunCommand (Commands.Reload, T)
            printfn "[%d <==== %A]" id res

            printfn "[%d ====> Close]" id 
            let! res = x.RunCommand (Commands.Close, T)
            printfn "[%d <==== %A]" id res
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

    let ``Test with 2 parallel async reply NO timeout`` () =
        let x = Boxing ()
       
        let pFlow timeout id x = async {
            do! Async.Sleep timeout
            return! flow id x
        }

        Async.Parallel [flow 1 x; pFlow 1000 2 x] |> Async.RunSynchronously

    let ``Test with 2 parallel async reply WITH timeout`` () =
        let x = Boxing ()
       
        let pFlowT pause id x T = async {
            do! Async.Sleep pause
            return! flowT id x T
        }
        let timeout = 1000
        let pause = 1000
        Async.Parallel [flowT 1 x timeout; pFlowT pause 2 x timeout] |> Async.RunSynchronously |> ignore
        printfn "[FINISHED]"

    let ``Test with 2 parallel sync requests`` () =
        let x = Boxing ()
       
        let pFlow pause id x = async {
            do! Async.Sleep pause
            return! flowSync id x
        }

        let timeout = 1000
        let pause = 1000
        Async.Parallel [flowSync 1 x; pFlow pause 2 x] |> Async.RunSynchronously |> ignore
        printfn "[FINISHED]"
        
    let ``Test with 3 parallel async reply WITH timeout`` () =
        let x = Boxing ()
       
        let pFlowT pause id x T = async {
            do! Async.Sleep pause
            return! flowT id x T
        }
        let timeout = 1000
        let pause = 500
        Async.Parallel [flowT 1 x timeout; pFlowT pause 2 x timeout; pFlowT (pause + 10) 3 x timeout] |> Async.RunSynchronously |> ignore
        printfn "[FINISHED]"

open ``Working with mailbox``
``Test with 3 parallel async reply WITH timeout`` ()


let startingBoard = [|[|1; 4; 7|];
                        [|6; 3; 5|];
                        [|0; 8; 2|]|]

let goal = [|[|1; 2; 3|];
                [|4; 5; 6|];
                [|7; 8; 0|]|]


let newpos (start : int[][]) (finish:int[][]) (i, j) = 
    let rw = 
        finish |> Array.fold (fun (found, y, x) row -> 
            if found then (found, y, x)
            else
                match row |> Array.tryFindIndex ((=) start.[i].[j]) with 
                | Some nX -> (true, y, nX) 
                | None -> (false, y+1, x)
        ) (false, 0, 0)
    
    match rw with
    | (true, x, y) -> (x, y)
    | _ -> failwith "Not found"


let totalManhattenDistance board goal =
    let manhattenDistance (x1, y1) (x2, y2) = abs(x1 - x2) + abs(y1 - y2)

    board |> Array.mapi (fun i arr ->
        arr |> Array.mapi (fun j v ->
            let (i1, j1) = newpos board goal (i, j)
            manhattenDistance (i, j) (i1, j1)
        ) |> Array.sum
    ) |> Array.sum 

totalManhattenDistance startingBoard goal