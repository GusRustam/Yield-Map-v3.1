namespace YieldMap.Core

module DbManager = 
    open Autofac
    open YieldMap.Database
    open YieldMap.Database.Access
    open YieldMap.Database.Procedures
    open YieldMap.Database.Procedures.Deletions
    open YieldMap.Database.Procedures.Additions

    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open System

    type DbManager (?_container : IContainer) = 
        let container = 
            match _container with
            | Some cont -> cont
            | None -> Initializer.Container 

        member internal x.db = container.Resolve<IDbConn> ()
        member internal x.bonds = container.Resolve<IBonds> ()
        member internal x.chainRics = container.Resolve<IChainRics> ()
        member internal x.ratings = container.Resolve<IRatings> ()
        member internal x.eraser = container.Resolve<IEraser> ()
        member internal x.chainLogic = container.Resolve<IChainsLogic> ()
        member internal x.refresh = container.Resolve<IRefresh> ()
        member internal x.backupRestore = container.Resolve<IBackupRestore> ()
        
        static member createContext (x : DbManager) = x.db.CreateContext ()
        static member chainsInNeed dt (x : DbManager) = x.refresh.ChainsInNeed dt
        static member needsRefresh dt (x : DbManager) = x.refresh.NeedsReload dt
        static member saveBonds bonds (x : DbManager) = x.bonds.Save bonds
        static member saveRatings ratings (x : DbManager) = x.ratings.SaveRatings ratings
        static member saveChainRics (x : DbManager) chainRic rics feedName expanded prms = 
            x.chainRics.SaveChainRics (chainRic, rics, feedName, expanded, prms)
        static member deleteRics selector (x : DbManager) = x.eraser.DeleteRics selector
        static member backup (x : DbManager) = x.backupRestore.Backup ()
        static member restore name (x : DbManager) = x.backupRestore.Restore name
        static member classify (x : DbManager) a b = x.chainLogic.Classify (a, b)

    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
        Database : DbManager
    }