namespace YieldMap.Tests.Unit

open System
open NUnit.Framework
open FsUnit

module DbTests =
    open YieldMap.Requests.MetaTables

    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory

    open YieldMap.Requests
    open YieldMap.Requests.MetaTables
    open YieldMap.Requests.Responses
        
    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Location
        
    open YieldMap.Database

    open System.IO

    let cnt boo = query { for x in boo do select x; count }

    let logger = LogFactory.create "UnitTests.TestDb"

    [<Test>]
    let ``Reading and writing to Db works`` () = 
        use ctx = Access.DbConn.CreateContext()

        let count = cnt ctx.Chains
        logger.InfoF "Da count is %d" count

        let c = Feed (Name = Guid.NewGuid().ToString())
        let c = ctx.Feeds.Add c
        logger.InfoF "First c is <%d; %s>" c.id c.Name
        ctx.SaveChanges() |> ignore

        logger.InfoF "Now c is <%d; %s>" c.id c.Name
        let poo =  cnt ctx.Feeds
        logger.InfoF "Da count is now %d" poo
        poo |> should equal (count+1)

        let c = ctx.Feeds.Remove (c)
        ctx.SaveChanges() |> ignore
        logger.InfoF "And now c is <%d; %s>" c.id c.Name
        let poo =  cnt ctx.Chains
        logger.InfoF "Da count is now %d" poo
        poo |> should equal count

    [<Test>]
    let ``Backup / restore`` () = 
        using (Access.DbConn.CreateContext()) (fun ctx ->
            cnt ctx.Feeds |> should be (equal 1))

        let addr = StoredProcedures.BackupRestore.Backup ()

        using (Access.DbConn.CreateContext()) (fun ctx ->
            cnt ctx.Feeds |> should be (equal 1))

        StoredProcedures.BackupRestore.Cleanup ()

        using (Access.DbConn.CreateContext()) (fun ctx ->
            cnt ctx.Feeds |> should be (equal 0))

        StoredProcedures.BackupRestore.Restore addr

        using (Access.DbConn.CreateContext()) (fun ctx ->
            cnt ctx.Feeds |> should be (equal 1))


    [<Test>]
    let ``Restore only`` () = 
        let b = StoredProcedures.BackupRestore.Backup ()

        try 
            StoredProcedures.BackupRestore.Restore <| Path.Combine(Location.path, "RUCORP.sql")
        with e -> 
            logger.ErrorEx "Failed to restore rucorp" e
            StoredProcedures.BackupRestore.Restore b