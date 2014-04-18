#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#endif

open System.Threading
open YieldMap.Tools.Aux

module ``Async extensions`` = 
    ////////////////////////////////////////////////////
    let someAsync () = async {
        for i in 1..10 do 
            do! Async.Sleep 100
            printfn "Thread %d, Value %d" Thread.CurrentThread.ManagedThreadId i
    }
        
    let testToken () = 
        printfn "=============================================="
        use token = new CancellationTokenSource ()
        let cAsync = someAsync () |> Async.WithCancellation token.Token
        cAsync |> Async.Ignore |> Async.Start 
        Thread.Sleep 500
        token.Cancel ()
        Thread.Sleep 500


    let testTimeout () =
        printfn "=============================================="
        let cAsync = someAsync () |> Async.WithTimeoutToken (Some 500)
        cAsync |> Async.Ignore |> Async.Start 
        Thread.Sleep 1000

    ////////////////////////////////////////////////////
    type Async with
        static member AutoCancelled<'T> timeout operation  = 
            let cancellableWork (tokenSource:CancellationTokenSource) = async {
                let task = Async.StartAsTask (operation, cancellationToken = tokenSource.Token)
                task.Wait ()
                return task.Result
            }

            let awaiter (tokenSource:CancellationTokenSource) = async {
                do! Async.Sleep timeout
                tokenSource.Cancel ()
                return Unchecked.defaultof<'T>
            }
            
            async {
                use tokenSource = new CancellationTokenSource() 
                let! res = [cancellableWork tokenSource; awaiter tokenSource] |> Async.Parallel 
                return res.[0]
            }

    let qqq = async {
        printfn "qqq" 
        try
            do! Async.Sleep 1000
            printfn "qqq bye" 
        with e -> printfn "qqq err" 
    }

    let tst () = qqq |> Async.AutoCancelled 500 |> Async.RunSynchronously
    ////////////////////////////////////////////////////

    type Async with
        static member WC (token : CancellationToken) operation = async {
            let task = Async.StartAsTask (operation, cancellationToken = token)
            task.Wait ()
            return task.Result
        }

    let testInternalCancellations () =
        let iF id = async {
            use! cancelHandler = Async.OnCancel(fun () -> printfn "Canceling IF%d" id)

            printfn "--> IF%d" id
            try 
                do! Async.Sleep 1000
            with e -> printfn "IF%d exn %s" id (e.GetType().Name)
            printfn "<-- IF%d" id
        }


        let generalFlow = async {
            printfn "--> generalFlow"

            use token = new CancellationTokenSource ()
            let iF id = 
                iF id 
                |> Async.WC token.Token 
                |> Async.Catch 
                |> Async.map (function Choice1Of2 res -> Some res | _ -> None)
            
            [ iF 1; iF 2; iF 3 ] 
            |> Async.Parallel 
            |> Async.Ignore 
            |> Async.Start

            do! Async.Sleep 100
            token.Cancel ()
            printfn "<-- generalFlow"
        }

        [iF 4; generalFlow]  
        |> Async.Parallel 
        |> Async.Ignore
        |> Async.RunSynchronously


open ``Async extensions``

testInternalCancellations ()
