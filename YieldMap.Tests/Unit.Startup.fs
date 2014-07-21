namespace YieldMap.Tests.Unit

open System
open System.IO

open NUnit.Framework
open FsUnit

module Ops = 
    open System.Data.Entity
    open YieldMap.Core.Startup
    open YieldMap.Database
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory
    open System.Data.Entity
    open System.Linq

    let cnt (table : 'a IQueryable) = 
        query { for x in table do 
                select x
                count }

    let checkData numChains dt (ctx : MainEntities) =
        cnt ctx.Feeds |> should be (equal 1)
        cnt ctx.Chains |> should be (equal numChains)

        ctx.Chains |> Seq.iter (fun ch -> ch.Expanded.Value |> should be (equal dt))

    type StartupTestParams = 
        {
            chains : string array
            date : DateTime
        }
        with override x.ToString () = sprintf "%s : %A" (x.date.ToString("dd/MM/yy")) x.chains

    let str (z : TimeSpan Nullable) = 
        if z.HasValue then z.Value.ToString("mm\:ss\.fffffff")
        else "N/A"

    let command cmd func state = 
        logger.InfoF "===> %s " cmd
        let res = func () |> Async.RunSynchronously  
        logger.InfoF " <=== %s : %s " cmd (res.ToString())
        res |> should be (equal state)


module StartupTest = 
    open Ops
    open Autofac

    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory

    open YieldMap.Core
    open YieldMap.Core.Loader
    open YieldMap.Core.Startup
    open YieldMap.Core.DbManager
    open YieldMap.Core.Notifier

    open YieldMap.Tools.Location
    open YieldMap.Tools.Logging
    open YieldMap.Database
    open YieldMap.Transitive.Repositories
    open YieldMap.Transitive.Queries
    open YieldMap.Transitive.Domains.Readers
    open YieldMap.Transitive.Domains.UnitsOfWork

    open System.Data.Entity
    open System.Linq

    open Clutch.Diagnostics.EntityFramework

    let logger = LogFactory.create "UnitTests.StartupTest"
    
    (* ========================= ============================= *)

    let paramsForStartup = [
        // todo some additional quanitities (how much items must be there in 
        { chains = [|"QQQQ"|]; date = DateTime(2014,5,14) } // invalid chain
        { chains = [|"0#RUCORP=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUEUROS="|]; date = DateTime(2014,5,14) } 
        { chains = [|"0#RUTSY=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUMOSB=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUSOVB=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RFGOVBONDS="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#USBMK=TWEB"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUEUROCAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUAER=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUBNK=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUBLD=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUCHE=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUELG=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#RUENR=MM"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#GBBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#EUBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#CNBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#JPBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#BRGLBBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#PAGLBBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#COGLBBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#COEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#MXGLBBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#MXEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#UAEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#BYEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#KZEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#AZEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#USBMK="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US1YT=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US2YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US3YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US5YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US7YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US10YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#US30YSTRIP=PX"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#EURO=DRGN"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#EUKZBYAZ=SBER"|]; date = DateTime(2014,5,14) }
        { chains = [|"0#GEEUROSAZ="|]; date = DateTime(2014,5,14) }
        { chains = [|"0#AM097464227="|]; date = DateTime(2014,5,14) }
        { chains = [|   "0#RUEUROS=";"0#RUTSY=MM";"0#RUCORP=MM";"0#RUMOSB=MM"; // duplicate 0#EUKZBYAZ=SBER
                        "0#RUSOVB=MM";"0#RFGOVBONDS=";"0#EUKZBYAZ=SBER";"0#USBMK=TWEB";
                        "0#RUEUROCAZ=";"0#RUAER=MM";"0#RUBNK=MM";"0#RUBLD=MM";"0#RUCHE=MM";
                        "0#RUELG=MM";"0#RUENR=MM";"0#GBBMK=";"0#EUBMK=";"0#CNBMK=";"0#JPBMK=";
                        "0#BRGLBBMK=";"0#PAGLBBMK=";"0#COGLBBMK=";"0#COEUROSAZ=";"0#MXGLBBMK=";
                        "0#MXEUROSAZ=";"0#UAEUROSAZ=";"0#BYEUROSAZ=";"0#KZEUROSAZ=";"0#AZEUROSAZ=";
                        "0#USBMK=";"0#US1YT=PX";"0#US2YSTRIP=PX";"0#US3YSTRIP=PX";"0#US5YSTRIP=PX";
                        "0#US7YSTRIP=PX";"0#US10YSTRIP=PX";"0#US30YSTRIP=PX";"0#EURO=DRGN";
                        "0#EUKZBYAZ=SBER";"0#GEEUROSAZ=";"0#AM097464227=" |]; date = DateTime(2014,5,14) }
        { chains = [|   "0#RUTSY=MM";"0#RUMOSB=MM";"0#RUSOVB=MM";"0#RFGOVBONDS=";"QDSDADS" |]; date = DateTime(2014,5,14) }
    ]

    let container = YieldMap.Transitive.DatabaseBuilder.Container


    let mutable (c : Calendar) = Unchecked.defaultof<Calendar>
    let mutable (s : Drivers) = Unchecked.defaultof<Drivers>
    let m = DbManager container

    let init chains dt = 
        c <- MockCalendar dt

        use uow = container.Resolve<IChainRicUnitOfWork>()
        use repo = container.Resolve<IChainRepository>(NamedParameter("uow", uow))

        chains |> Array.iter (fun name -> 
            if not <| repo.FindBy(fun (x:Chain) -> x.Name = name).Any() then
                repo.Add <| Chain(Name = name, id_Feed = Nullable(1L), Params = "", State = State.Added) |> ignore)
        uow.Save () |> ignore 

        s <- {
            Factory = MockFactory ()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
            Database = m
        }

        Startup s

    [<SetUp>]
    let setup () = 
        let finish (c : DbTracingContext) = logger.TraceF "Finished : %s %s" (str c.Duration) (c.Command.ToTraceString())
        let failed (c : DbTracingContext) = logger.ErrorF "Failed : %s %s" (str c.Duration) (c.Command.ToTraceString())
        DbTracing.Enable(GenericDbTracingListener().OnFinished(Action<_>(finish)).OnFailed(Action<_>(failed)))

        globalThreshold := LoggingLevel.Debug

    [<TearDown>]
    let teardown () = 
        globalThreshold := LoggingLevel.Warn
        logger.Info "teardown"
        DbTracing.Disable ()
        m |> DbManager.restore "EMPTY.sql"

    [<Test>]
    [<TestCaseSource("paramsForStartup")>]
    let ``Startup with one chain`` xxx =             

        let { date = dt; chains = prms } = xxx
        logger.WarnF "Starting test with chains %A" prms
        
        let x = init prms dt
        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
            let m =
                match severity with
                | Evil -> logger.ErrorF
                | Warn -> logger.TraceF
                | Note -> logger.InfoF

            m "MSG: @%A %s" state (fail.ToString())
        )

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        command "Connect" x.Connect (Startup.State Initialized)

        logger.Error " =============== SECOND RELOAD ====================="
        command "Reload" (fun () -> x.Reload true) (Startup.State Initialized)
        command "Close" x.Close (Startup.State Closed)

        checkData (Array.length prms) dt

    (* ========================= ============================= *)
    [<Test>]
    let ``Simple startup and states`` () =
        let dt = DateTime(2014,5,14) 
        let prms = "0#RUELG=MM"
        
        let x = init [|prms|] dt
        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
            let m =
                match severity with
                | Evil -> logger.ErrorF
                | Warn -> logger.TraceF
                | Note -> logger.InfoF

            m "MSG: @%A %s" state (fail.ToString())
        )

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        command "Connect" x.Connect (Startup.State Initialized)

        logger.Error " =============== SECOND RELOAD ====================="
        command "Reload" (fun () -> x.Reload true) (Startup.State Initialized)
        command "Close" x.Close (Startup.State Closed)
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding

        let feeds = container.Resolve<IFeedRepository>().FindAll()
        let chains = container.Resolve<IChainRepository>().FindAll()
        
        cnt feeds |> should be (equal 1)
        cnt chains  |> should be (equal 1)

        chains |> Seq.iter (fun ch -> ch.Expanded.Value |> should be (equal dt))


    (* ========================= ============================= *)
    [<Test>]
    let ``Duplicate Isin leave no RIC unlinked`` () =
        let dt = DateTime(2014,5,14) 
        let x = init [|"0#RUEUROS="|] dt        
        
        use ctx = container.Resolve<IRicRepository>()
        let initialUnattachedRics = query {
            for n in ctx.FindAll() do
            where (n.Isin = null)
            count}

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload true) (Startup.State Initialized)

        let unattachedRics = query {
            for n in ctx.FindAll() do
            where (n.Isin = null)
            count
        }

        unattachedRics |> should be (equal initialUnattachedRics)


    (* ========================= ============================= *)
    [<Test>]
    let ``Strange US30`` () =
        let dt = DateTime(2014,5,14) 
        let x = init [|"0#US30YSTRIP=PX"|] dt

        use ctx = container.Resolve<IRicRepository>()
        let initialUnattachedRics = query {
            for n in ctx.FindAll() do
            where (n.Isin = null)
            count}

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        command "Connect" x.Connect (Startup.State Initialized)

        let unattachedRics = query {
            for n in ctx.FindAll() do
            where (n.Isin = null)
            select n}

        let unattachedRics = unattachedRics.ToArray()

        // RIC US912834NP9=PX exists in chains, but doesn't exist in bond database for some reason. And that's ok
        (Array.length unattachedRics - initialUnattachedRics) |> should be (equal 1)

        let loser = unattachedRics.[0]
        match unattachedRics |> Array.tryFindIndex (fun t -> t.Name = "US912834NP9=PX") with
        | None -> true |> should be (equal false)
        | _ -> ()

    open YieldMap.Tools.Aux

    (* ========================= ============================= *)
    [<Test>]
    let ``RUCORP overnight`` () =
        // Preparing
        let dt = DateTime(2014,5,14,23,0,0)
        use c = new UpdateableCalendar (dt)
        let clndr = c :> Calendar

        // Preparing
        s <- {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
            Database = m
        }

        let x = Startup s
        
        use uow = container.Resolve<IChainRicUnitOfWork>()
        use repo = container.Resolve<IChainRepository>(NamedParameter("uow", uow))

        repo.Add <| Chain(Name = "0#RUCORP=MM", id_Feed = Nullable(1L), Params = "", State = State.Added) |> ignore
        uow.Save () |> ignore

        command "Connect" x.Connect (Startup.State Connected)
        logger.Info "Reloading"
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        logger.Info "Reloaded"

        let assertAmounts dt t o r k =
            // todo secure proprtions
            let proportions = 
                DbManager.classify s.Database dt [||]
                |> Map.fromDict
                |> Map.map (fun _ -> Array.length)

            logger.InfoF "%A" proportions

            let total = 
                proportions.[Mission.Obsolete] +
                proportions.[Mission.ToReload] +
                proportions.[Mission.Keep]

            use ctx = container.Resolve<IInstrumentRepository>()
            let totalCount = query { for x in ctx.FindAll() do
                                     count }

            total |> should be (equal t)
            totalCount |> should be (equal t)

            proportions.[Mission.Obsolete] |> should be (equal o)
            proportions.[Mission.ToReload] |> should be (equal r)
            proportions.[Mission.Keep] |> should be (equal k)

        assertAmounts dt 923 8 48 867

        let e = Event<_> ()
        let ep = e.Publish

        logger.Info "Setting time"
        let dt = DateTime(2014,5,14,23,59,55)
        c.SetTime dt

        logger.InfoF "And time is %s" (clndr.Now.ToString("dd-MMM-yy"))

        logger.Info "Adding handler time"
        clndr.NewDay |> Observable.add (fun dt ->
            assertAmounts dt 923 8 48 867

            command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
            logger.Info "Reloaded2"

            e.Trigger ()
        )

        let awaitForReload = async {
            logger.Info "ReloadAwaiter: Waiting for reload to start and finish"
            // now wait 5 secs until tomorrow
            do! Async.AwaitEvent ep |> Async.WithTimeoutEx (Some (5*60*1000))
            logger.Info "ReloadAwaiter: Yesss!"
        }

        let errorCount = awaitForReload |> Async.Catch|> Async.RunSynchronously

        let ec = 
            match errorCount with
            | Choice1Of2 _ -> 0 
            | Choice2Of2 e -> logger.ErrorEx "Error" e; 1

        ec |> should be (equal 0)

    (* ========================= ============================= *)
    [<Test>]
    let ``Indexes and FRNs`` () =
        let dt = DateTime(2014,5,14) 
        let x = init [|"0#RUCORP=MM"|] dt

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        use ctx = container.Resolve<IOrdinaryFrnReader>()
        let n = query { for x in ctx.FindAll() do
                        select x
                        count }

        n |> should be (equal 22)

    (* ========================= ============================= *)
    [<Test>]
    let ``Utility. Initialize Db with 0#RUCORP=MM`` () =
        let dt = DateTime(2014,5,14) 
        let x = init [|"0#RUCORP=MM"|] dt

        command "Connect" x.Connect (Startup.State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (Startup.State Initialized)
        
        s.Database 
        |> DbManager.backup 
        |> logger.Info