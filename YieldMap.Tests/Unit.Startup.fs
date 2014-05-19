namespace YieldMap.Tests.Unit

open System
open System.IO

open NUnit.Framework
open FsUnit

module Ops = 
    open System.Data.Entity
    open YieldMap.Database

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

    let logger = LogFactory.create "StartupTest"
    
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

        let str (z : TimeSpan Nullable) = 
            if z.HasValue then z.Value.ToString("mm\:ss\.fffffff")
            else "N/A"
             
        let finish (c : DbTracingContext) = logger.TraceF "Finished : %s %s" (str c.Duration) (c.Command.ToTraceString())
        let failed (c : DbTracingContext) = logger.ErrorF "Failed : %s %s" (str c.Duration) (c.Command.ToTraceString())
        
        DbTracing.Enable(GenericDbTracingListener().OnFinished(Action<_>(finish)).OnFailed(Action<_>(failed)))

        let { date = dt; chains = prms } = xxx

        logger.WarnF "Starting test with chains %A" prms
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        let eraser = new Eraser ()
        eraser.DeleteChains ()
        eraser.DeleteBonds ()
        eraser.DeleteFeeds ()
        eraser.DeleteIsins ()
        eraser.DeleteRics ()

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
        )
        ctx.SaveChanges () |> ignore

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta dt
            Calendar = MockCalendar dt
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
        let eraser = new Eraser ()
        eraser.DeleteChains ()
        eraser.DeleteBonds ()
        eraser.DeleteFeeds ()
        eraser.DeleteIsins ()
        eraser.DeleteRics ()

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

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta dt
            Calendar = MockCalendar dt
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
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding

        use ctx = DbConn.CreateContext()
        cnt ctx.Feeds |> should be (equal 1)
        cnt ctx.Chains |> should be (equal 1)

        ctx.Chains |> Seq.iter (fun ch -> ch.Expanded.Value |> should be (equal dt))


    (* ========================= ============================= *)
    [<Test>]
    let ``Duplicate isin leave no RIC unlinked`` () =
        let dt = DateTime(2014,5,14) 
        
        globalThreshold := LoggingLevel.Debug

        // Cleaning up db
        let eraser = new Eraser ()
        eraser.DeleteChains ()
        eraser.DeleteBonds ()
        eraser.DeleteFeeds ()
        eraser.DeleteIsins ()
        eraser.DeleteRics ()

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

        // Preparing
        let x = Startup {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta dt
            Calendar = MockCalendar dt
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
            count
        }

        unattachedRics |> should be (equal 0)


    // todo reload on overnight