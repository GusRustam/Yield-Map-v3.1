namespace YieldMap.Loader.SdkFactory

[<AutoOpen>]
module SdkFactory =

    open AdfinXAnalyticsFunctions
    open Dex2
    open EikonDesktopDataAPI
    open ThomsonReuters.Interop.RTX
    
    open YieldMap.Loader.Requests
    open YieldMap.Tools.Logging
    
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

    /// 
    type EikonFactory = 
        abstract member Connect : unit -> Async<Connection>
        abstract member CreateAdxBondModule : unit -> AdxBondModule
        abstract member CreateAdxRtChain : unit -> AdxRtChain
        abstract member CreateAdxRtList : unit -> AdxRtList
        abstract member CreateDex2Mgr : unit -> Dex2Mgr

    type OuterFactory(_eikon:EikonDesktopDataAPI)  =
        new (_eikon) = OuterFactory(_eikon)
        interface EikonFactory with
            member x.Connect () = EikonOperations.connect _eikon
            member x.CreateAdxBondModule () = _eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = _eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateAdxRtList () = _eikon.CreateAdxRtList() :?> AdxRtList
            member x.CreateDex2Mgr () = _eikon.CreateDex2Mgr() :?> Dex2Mgr

    type InnerFactory()  =
        interface EikonFactory with
            member x.Connect () = async { 
                do! Async.Sleep(500) 
                return Connected
            }
            member x.CreateAdxBondModule () = AdxBondModuleClass() :> AdxBondModule
            member x.CreateAdxRtChain () = new AdxRtChainClass() :> AdxRtChain
            member x.CreateAdxRtList () = new AdxRtListClass() :> AdxRtList
            member x.CreateDex2Mgr () =  new Dex2MgrClass() :> Dex2Mgr

    type MockFactory()  =
        interface EikonFactory with
            member x.Connect () = async {
                do! Async.Sleep(500) 
                return Connected
            }
            member x.CreateAdxBondModule () = null
            member x.CreateAdxRtChain () = null
            member x.CreateAdxRtList () = null
            member x.CreateDex2Mgr () = null
