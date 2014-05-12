namespace YieldMap.Loader.SdkFactory

[<AutoOpen>]
module SdkFactory =

    open AdfinXAnalyticsFunctions
    open Dex2
    open EikonDesktopDataAPI
    open ThomsonReuters.Interop.RTX
    
    open YieldMap.Requests
    open YieldMap.Requests.Responses
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Aux
    
    let private logger = LogFactory.create "SdkFactory"

    module private EikonOperations = 
        type Eikon(eikon : EikonDesktopDataAPI) =
            let changed = new Event<_>()
            do eikon.add_OnStatusChanged (fun e -> 
                logger.TraceF "Status changed -> %A!" e
                changed.Trigger <| Connection.parse e)
            member self.StatusChanged = changed.Publish

        let connect (eikon : EikonDesktopDataAPI) = 
            let watcher = Eikon eikon
            let res = eikon.Initialize()
            Async.AwaitEvent watcher.StatusChanged

    type q = member x.foo (?z) = z

    /// 
    type EikonFactory = 
        abstract OnConnectionStatus : Ping IEvent
        abstract Connect : ?t:int -> Ping Async
        abstract CreateAdxBondModule : unit -> AdxBondModule
        abstract CreateAdxRtChain : unit -> AdxRtChain
        abstract CreateAdxRtList : unit -> AdxRtList
        abstract CreateDex2Mgr : unit -> Dex2Mgr

    type OuterFactory(_eikon:EikonDesktopDataAPI)  =
        let watcher = EikonOperations.Eikon _eikon
        let connStatus = new Event<_>()
        do watcher.StatusChanged |> Observable.add connStatus.Trigger

        new (_eikon) = OuterFactory(_eikon)
        interface EikonFactory with
            member x.OnConnectionStatus = connStatus.Publish
            member x.Connect ?timeout = 
                async {
                    let! res = EikonOperations.connect _eikon |> Async.WithTimeout timeout
                    return match res with Some ans -> ans | None -> Ping.Failure Timeout
                }
            member x.CreateAdxBondModule () = _eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = _eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateAdxRtList () = _eikon.CreateAdxRtList() :?> AdxRtList
            member x.CreateDex2Mgr () = _eikon.CreateDex2Mgr() :?> Dex2Mgr

    let private doConnect (connStatus : Ping Event) timeout = async { 
        match timeout with
        | Some n when n < 500 -> return Ping.Failure Timeout
        | _ -> 
            do! Async.Sleep(500) 
            connStatus.Trigger <| Ping.Answer ()
            return Ping.Answer ()
    }

    type InnerFactory()  =
        // todo How to determine if Eikon itself has lost connection or is in local mode or whatever?
        let connStatus = new Event<_>()
        interface EikonFactory with
            member x.OnConnectionStatus = connStatus.Publish
            member x.Connect ?timeout = doConnect connStatus timeout
            member x.CreateAdxBondModule () = AdxBondModuleClass() :> AdxBondModule
            member x.CreateAdxRtChain () = AdxRtChainClass() :> AdxRtChain
            member x.CreateAdxRtList () = AdxRtListClass() :> AdxRtList
            member x.CreateDex2Mgr () =  Dex2MgrClass() :> Dex2Mgr

    type MockFactory()  =
        let connStatus = new Event<_>()
        
        member x.Disconnect () = connStatus.Trigger <| Ping.Failure (Failure.Problem "Disconnected")

        interface EikonFactory with
            member x.OnConnectionStatus = connStatus.Publish
            member x.Connect ?timeout = doConnect connStatus timeout
            member x.CreateAdxBondModule () = null
            member x.CreateAdxRtChain () = null
            member x.CreateAdxRtList () = null
            member x.CreateDex2Mgr () = null
