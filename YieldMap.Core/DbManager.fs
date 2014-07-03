namespace YieldMap.Core

module DbManager = 
    open Autofac
    open YieldMap.Transitive.Domains.Procedures
    open YieldMap.Transitive.Domains.Queries

    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open System

    type DbManager (container : IContainer) = 
//        let container = 
//            match _container with
//            | Some cont -> cont
//            | None -> Initializer.Container 

        member internal x.bonds = container.Resolve<IBonds> ()
        member internal x.chainRics = container.Resolve<IChainRics> ()
        member internal x.ratings = container.Resolve<IRatings> ()
        member internal x.updates = container.Resolve<IUpdatesRepository> ()
        member internal x.backupRestore = container.Resolve<IBackupRestore> ()
        
        static member chainsInNeed dt (x : DbManager) = x.updates.ChainsInNeed dt
        static member needsRefresh dt (x : DbManager) = x.updates.NeedsReload dt
        static member classify (x : DbManager) a b = x.updates.Classify (a, b)
        static member saveBonds bonds (x : DbManager) = x.bonds.Save bonds
        static member saveRatings ratings (x : DbManager) = x.ratings.SaveRatings ratings
        static member saveChainRics (x : DbManager) chainRic rics feedName expanded prms = 
            x.chainRics.SaveChainRics (chainRic, rics, feedName, expanded, prms)
        static member backup (x : DbManager) = x.backupRestore.Backup ()
        static member restore name (x : DbManager) = x.backupRestore.Restore name

    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
        Database : DbManager
    }