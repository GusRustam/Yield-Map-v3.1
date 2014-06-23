namespace YieldMap.Core

module Manager = 
    open Autofac
    open YieldMap.Database
    open YieldMap.Database.Access
    open YieldMap.Database.StoredProcedures
    open YieldMap.Database.StoredProcedures.Deletions
    open YieldMap.Database.StoredProcedures.Additions

    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open System

    type Manager (?_container : IContainer) = 
        let container = 
            match _container with
            | Some cont -> cont
            | None -> Manager.Container 

        member x.db = container.Resolve<IDbConn> ()
        member x.bonds = container.Resolve<IBonds> ()
        member x.chainRics = container.Resolve<IChainRics> ()
        member x.ratings = container.Resolve<IRatings> ()
        member x.eraser = container.Resolve<IEraser> ()
        member x.chainLogic = container.Resolve<IChainsLogic> ()
        member x.refresh = container.Resolve<IRefresh> ()
        member x.backupRestore = container.Resolve<IBackupRestore> ()

        member x.chainsInNeed = x.refresh.ChainsInNeed
        member x.needsRefresh = x.refresh.NeedsReload 
        member x.backup = x.backupRestore.Backup
        member x.restore = x.backupRestore.Restore

        member x.classify a b = x.chainLogic.Classify (a, b)


    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
        Database : Manager
    }