namespace YieldMap.Core

module DbManager = 
    open Autofac
    open YieldMap.Transitive.Procedures
    open YieldMap.Transitive.Queries

    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open System


    type DbManager (container : IContainer) = 
        member internal __.bonds = container.Resolve<IBonds> ()
        member internal __.chainRics = container.Resolve<IChainRics> ()
        member internal __.ratings = container.Resolve<IRatings> ()
        member internal __.updates = container.Resolve<IDbUpdates> ()
        member internal __.backupRestore = container.Resolve<IBackupRestore> ()

        member __.dbContainer = container
       
        static member chainsInNeed dt (x : DbManager) = x.updates.ChainsInNeed dt
        static member needsRefresh dt (x : DbManager) = x.updates.NeedsReload dt
        static member classify (x : DbManager) a b = x.updates.Classify (a, b)
        static member saveBonds bonds (x : DbManager) = x.bonds.Save bonds
        static member saveRatings ratings (x : DbManager) = x.ratings.Save ratings
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