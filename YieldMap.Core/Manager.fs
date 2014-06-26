namespace YieldMap.Core

module Manager = 
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

    type Manager (?_container : IContainer) = 
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
        
        static member createContext (x : Manager) = x.db.CreateContext ()
        static member chainsInNeed dt (x : Manager) = x.refresh.ChainsInNeed dt
        static member needsRefresh dt (x : Manager) = x.refresh.NeedsReload dt
        static member saveBonds bonds (x : Manager) = x.bonds.Save bonds
        static member saveRatings ratings (x : Manager) = x.ratings.SaveRatings ratings
        static member saveChainRics (x : Manager) chainRic rics feedName expanded prms = 
            x.chainRics.SaveChainRics (chainRic, rics, feedName, expanded, prms)
        static member deleteRics selector (x : Manager) = x.eraser.DeleteRics selector
        static member backup (x : Manager) = x.backupRestore.Backup ()
        static member restore name (x : Manager) = x.backupRestore.Restore name
        static member classify (x : Manager) a b = x.chainLogic.Classify (a, b)

    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
        Database : Manager
    }