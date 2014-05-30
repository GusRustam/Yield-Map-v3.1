namespace YieldMap.Tests.Unit

open System
open System.IO

open NUnit.Framework
open FsUnit

module Ops = 
    open System.Data.Entity
    open YieldMap.Database
    open YieldMap.Database.StoredProcedures

    let cleanup () =
        // Cleaning up db
        let eraser = new Eraser ()
        eraser.DeleteChains ()
        eraser.DeleteInstruments () // todo why???
        eraser.DeleteFeeds ()
        eraser.DeleteIsins ()
        eraser.DeleteRics ()

    let cnt (table : 'a DbSet) = 
        query { for x in table do 
                select x
                count }

    let checkData numChains dt =
        use ctx = Access.DbConn.CreateContext()
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

module StartupTest = 
    open Ops

    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory

    open YieldMap.Core.Application
    open YieldMap.Core.Application.Operations
    open YieldMap.Core.Application.Startup
    open YieldMap.Core.Notifier

    open YieldMap.Database
    open YieldMap.Database.Access
    open YieldMap.Database.StoredProcedures 

    open YieldMap.Tools.Location
    open YieldMap.Tools.Logging

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

    [<Test>]
    [<TestCaseSource("paramsForStartup")>]
    let ``Startup with one chain`` xxx =             
        let finish (c : DbTracingContext) = logger.TraceF "Finished : %s %s" (str c.Duration) (c.Command.ToTraceString())
        let failed (c : DbTracingContext) = logger.ErrorF "Failed : %s %s" (str c.Duration) (c.Command.ToTraceString())
        
        DbTracing.Enable(GenericDbTracingListener().OnFinished(Action<_>(finish)).OnFailed(Action<_>(failed)))

        let { date = dt; chains = prms } = xxx

        logger.WarnF "Starting test with chains %A" prms
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        cleanup ()

        // Checking cleanup
        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 0)
        cnt ctx.Chains |> should be (equal 0)
        cnt ctx.Rics |> should be (equal 0)
        cnt ctx.Isins |> should be (equal 0)

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        prms |> Array.iter (fun name -> 
            if not <| ctx.Chains.Any(fun (x:Chain) -> x.Name = name) then
                ctx.Chains.Add <| Chain(Name = name, Feed = idn, Params = "") |> ignore
                ctx.SaveChanges () |> ignore
        )

        // Preparing
        let c = MockCalendar dt

        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
        }

        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
            let m =
                match severity with
                | Evil -> logger.ErrorF
                | Warn -> logger.TraceF
                | Note -> logger.InfoF

            m "MSG: @%A %s" state (fail.ToString())
        )

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        command "Connect" x.Connect (State Initialized)

        logger.Error " =============== SECOND RELOAD ====================="
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Close" x.Close (State Closed)

        checkData (Array.length prms) dt

    (* ========================= ============================= *)
    [<Test>]
    let ``Simple startup and states`` () =
        let dt = DateTime(2014,5,14) 
        let prms = "0#RUTSY=MM"
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        cleanup ()

        // Checking cleanup
        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 0)
        cnt ctx.Chains |> should be (equal 0)
        cnt ctx.Rics |> should be (equal 0)
        cnt ctx.Isins |> should be (equal 0)

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = prms, Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

        let c  = MockCalendar dt

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
        }

        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
            let m =
                match severity with
                | Evil -> logger.ErrorF
                | Warn -> logger.TraceF
                | Note -> logger.InfoF

            m "MSG: @%A %s" state (fail.ToString())
        )

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
//        command "Connect" x.Connect (State Initialized)
//
//        logger.Error " =============== SECOND RELOAD ====================="
//        command "Reload" (fun () -> x.Reload true) (State Initialized)
//        command "Close" x.Close (State Closed)
//        command "Close" x.Close NotResponding
//        command "Connect" x.Connect NotResponding

        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 1)
        cnt ctx.Chains |> should be (equal 1)

        ctx.Chains |> Seq.iter (fun ch -> ch.Expanded.Value |> should be (equal dt))


    (* ========================= ============================= *)
    [<Test>]
    let ``Duplicate Isin leave no RIC unlinked`` () =
        let dt = DateTime(2014,5,14) 
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        cleanup ()

        // Checking cleanup
        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 0)
        cnt ctx.Chains |> should be (equal 0)
        cnt ctx.Rics |> should be (equal 0)
        cnt ctx.Isins |> should be (equal 0)

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = "0#RUEUROS=", Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

        let c  = MockCalendar dt

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
        }

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload true) (State Initialized)

        use ctx = DbConn.CreateContext()

        let unattachedRics = query {
            for n in ctx.Rics do
            where (n.Isin = null)
            count
        }

        unattachedRics |> should be (equal 0)


    (* ========================= ============================= *)
    [<Test>]
    let ``Strange US30`` () =
        let dt = DateTime(2014,5,14) 
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        cleanup ()

        // Checking cleanup
        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 0)
        cnt ctx.Chains |> should be (equal 0)
        cnt ctx.Rics |> should be (equal 0)
        cnt ctx.Isins |> should be (equal 0)

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = "0#US30YSTRIP=PX", Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

        let c  = MockCalendar dt

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
        }

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        command "Connect" x.Connect (State Initialized)

        use ctx = DbConn.CreateContext()

        let unattachedRics = query {
            for n in ctx.Rics do
            where (n.Isin = null)
            select n
        }

        let unattachedRics = unattachedRics.ToArray()

        // RIC US912834NP9=PX exists in chains, but doesn't exist in bond database for some reason. And that's ok
        unattachedRics |> Array.length |> should be (equal 1)

        let loser = unattachedRics.[0]
        loser.Name |> should be (equal "US912834NP9=PX")

    open YieldMap.Tools.Aux

    (* ========================= ============================= *)
    [<Test>]
    let ``RUCORP overnight`` () =

        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        let eraser = new Eraser ()
        cleanup ()

        // Checking cleanup
        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 0)
        cnt ctx.Chains |> should be (equal 0)
        cnt ctx.Rics |> should be (equal 0)
        cnt ctx.Isins |> should be (equal 0)

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = "0#RUCORP=MM", Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

        // Preparing
        let dt = DateTime(2014,5,14,23,0,0)
        use c = new UpdateableCalendar (dt)
        let clndr = c :> Calendar

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
        }

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        logger.Info "Reloading"
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        logger.Info "Reloaded"

        // todo secure proprtions
        let proportions = 
            ChainsLogic.Classify(dt, [||]) 
            |> Map.fromDict
            |> Map.map (fun _ -> Array.length)

        logger.InfoF "%A" proportions

        let total = 
            proportions.[Mission.Obsolete] +
            proportions.[Mission.ToReload] +
            proportions.[Mission.Keep]

        let totalCount = query { for x in ctx.Instruments do
                                 count }

        total |> should be (equal totalCount)

        proportions.[Mission.Obsolete] |> should be (equal 8)
        proportions.[Mission.ToReload] |> should be (equal 49)
        proportions.[Mission.Keep] |> should be (equal 872)

        let e = Event<_> ()
        let ep = e.Publish

        logger.Info "Setting time"
        let dt = DateTime(2014,5,14,23,59,55)
        c.SetTime dt

        logger.InfoF "And time is %s" (clndr.Now.ToString("dd-MMM-yy"))

        // now wait 5 secs until tomorrow
        logger.Info "Adding handler time"
        clndr.NewDay |> Observable.add (fun dt ->

            logger.InfoF "OnNewDay: Reloading second time at %s" (dt.ToString("dd-MMM-yy hh:mm:ss"))
            // todo secure proprtions
            let proportions = 
                ChainsLogic.Classify(dt, [||]) 
                |> Map.fromDict
                |> Map.map (fun _ -> Array.length)
                
            logger.InfoF "%A" proportions

            let total = 
                proportions.[Mission.Obsolete] +
                proportions.[Mission.ToReload] +
                proportions.[Mission.Keep]

            let totalCount = query { for x in ctx.Instruments do
                                     count }

            total |> should be (equal totalCount)


            proportions.[Mission.Obsolete] |> should be (equal 8)
            proportions.[Mission.ToReload] |> should be (equal 49)
            proportions.[Mission.Keep] |> should be (equal 872)

            command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
            logger.Info "Reloaded2"

            e.Trigger ()
        )

        let awaitForReload = async {
            logger.Info "ReloadAwaiter: Waiting for reload to start and finish"
            do! Async.AwaitEvent ep |> Async.WithTimeoutEx (Some (5*60*1000))
            logger.Info "ReloadAwaiter: Yesss!"
        }

        let errorCount = 
            awaitForReload
                |> Async.Catch
                |> Async.RunSynchronously

        let ec = 
            match errorCount with
            | Choice1Of2 _ -> 0 
            | Choice2Of2 e -> logger.ErrorEx "Error" e; 1

        ec |> should be (equal 0)


    (* ========================= ============================= *)
    [<Test>]
    let ``Indexes and FRNs`` () =
        let dt = DateTime(2014,5,14) 
        
        globalThreshold := LoggingLevel.Debug

        cleanup ()

        use ctx = DbConn.CreateContext ()
        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = "0#RUCORP=MM", Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

        // Preparing
        let c  = MockCalendar dt
        let x = Startup { Factory = MockFactory(); TodayFix = dt; Loader = MockChainMeta c; Calendar = c }

        let command cmd func state = func () |> Async.RunSynchronously |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)

        use ctx = DbConn.CreateContext ()
        let n = query { for x in ctx.OrdinaryFrns do
                        select x
                        count }

        n |> should be (equal 22)
        