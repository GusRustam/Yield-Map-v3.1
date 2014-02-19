#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map\Tools\YieldMapDsl\YieldMapDsl\bin\Debug\YieldMapDsl.dll"
#endif

module ``18-Feb-2014 - testing metaloader`` =
    open System
    open System.IO
    open System.Xml

    open EikonDesktopDataAPI

    open YieldMap.Data.Answers
    open YieldMap.Data.Requests
    open YieldMap.Data.Loading
    open YieldMap.Data.MetaTables
    open YieldMap.Data

    let doIt (q:MetaLoader) = async {
        printfn "Connection request sent"
        let! connectRes = q.Connect()
        match connectRes with
        | Connection.Connected -> 
            let! chain = q.LoadChain { Feed = "IDN"; Mode = ""; Ric = "0#RUCORP=MM" }
            match chain with
            | Chain.Answer data -> 
                printfn "Chain %A" data
                let! meta = q.LoadMetadata<BondDescr> data
                match meta with
                | Meta.Answer metaData -> printfn "BondDescr is %A" metaData
                | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()

                let! meta = q.LoadMetadata<CouponData> data
                match meta with
                | Meta.Answer metaData -> printfn "CouponData is %A" metaData
                | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()
            | Chain.Failed e -> printfn "Failed to load chain: %s" e.Message
        | Connection.Failed e -> printfn "Failed to connect %s" <| e.ToString()
    } 

    let test () = 
        let q = MockLoader() :> MetaLoader
        //let e = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI
        //let q = OuterLoader(e) :> MetaLoader

        doIt q |> Async.Start

module ``19-Feb-2014 - can't receive connection to Eikon in async mode`` =
    open EikonDesktopDataAPI

    open System

    type EikonWatcher(eikon : EikonDesktopDataAPI) =
        let changed = new Event<_>()
        do eikon.add_OnStatusChanged (fun e -> 
            printfn "Kookoooo"
            changed.Trigger true)
        member self.StatusChanged = changed.Publish

    let ``will that connection work?`` () = 
        let eikon = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI
        
        let a = async {
//            use ctx = new System.Windows.Forms.WindowsFormsSynchronizationContext()
//            do! Async.SwitchToContext ctx

//            do! Async.SwitchToThreadPool()

            let watcher = EikonWatcher eikon
            let res = eikon.Initialize()
            printfn "res is %A" res

//            let! token = Async.StartChildAsTask(Async.AwaitEvent watcher.StatusChanged)
            let! result =  Async.AwaitEvent watcher.StatusChanged
            printfn "%A" result
            return result
//            token.Wait()
//            return token.Result
        } 
        
        Async.Start (Async.Ignore a)
//        Async.RunSynchronously (Async.Ignore a)


``19-Feb-2014 - can't receive connection to Eikon in async mode``.``will that connection work?`` ()
