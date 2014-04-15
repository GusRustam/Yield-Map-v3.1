#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#endif

open System.Threading
open YieldMap.Tools.Aux

module ``Async extensions`` = 
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

open ``Async extensions``

testToken () 
testTimeout ()