namespace YieldMap.Core

module Manager = 
    open Autofac
    open YieldMap.Database
    open YieldMap.Database.Access
    open YieldMap.Database.StoredProcedures
    open YieldMap.Database.StoredProcedures.Deletions
    open YieldMap.Database.StoredProcedures.Additions

    let private container = Manager.Container

    let db = container.Resolve<IDbConn> ()
    let bonds = container.Resolve<IBonds> ()
    let chainRics = container.Resolve<IChainRics> ()
    let ratings = container.Resolve<IRatings> ()
    let eraser = container.Resolve<IEraser> ()
    let chainLogic = container.Resolve<IChainsLogic> ()
    let refresh = container.Resolve<IRefresh> ()
    let backupRestore = container.Resolve<IBackupRestore> ()

    let chainsInNeed = refresh.ChainsInNeed
    let needsRefresh = refresh.NeedsReload 
    let backup = backupRestore.Backup
    let restore = backupRestore.Restore

    let classify a b = chainLogic.Classify (a, b)