namespace YieldMap.Tests.Unit

open System
open System.IO

open NUnit.Framework
open FsUnit

module StartupTest = 
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory

    open YieldMap.Core.Application
    open YieldMap.Core.Application.Startup
    open YieldMap.Core.Notifier

    open YieldMap.Database

    open YieldMap.Tools.Location
    open YieldMap.Tools.Logging

    open System.Data.Entity

    let logger = LogFactory.create "StartupTest"
    
    [<TestCase>]
    let ``Simple workflow with empty database`` () =
        let dt = DateTime(2014,3,4)
                
        let zzz = {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta dt
            Calendar = MockCalendar dt
        }

        let x = Startup zzz

        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
            let m =
                match severity with
                | Evil -> logger.ErrorF
                | Warn -> logger.TraceF
                | Note -> logger.InfoF

            m  "MSG: @%A %s" state (fail.ToString())
        )

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Connect" x.Connect (State Initialized)
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Close" x.Close (State Closed)
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding


    MainEntities.SetVariable("PathToTheDatabase", Location.path)
    let cnnStr = MainEntities.GetConnectionString("TheMainEntities")

    let rec clear (ctx:MainEntities) (table : 'a DbSet) =
        let item = query { for x in table do 
                            select x
                            exactlyOneOrDefault }
        if item <> null then
            table.Remove item |> ignore
            ctx.SaveChanges () |> ignore
            clear ctx table
                
                
    let initDb () = 
        use ctx = new MainEntities(cnnStr)

        clear ctx ctx.Feeds
        clear ctx ctx.Chains

        let idn = ctx.Feeds.Add <| Feed(Name = "Q")
        ctx.SaveChanges () |> ignore

        ctx.Chains.Add <| Chain(Name = "0#RUAER=MM", Feed = idn, Params = "") |> ignore
        ctx.SaveChanges () |> ignore

    let checkData () =
        use ctx = new MainEntities(cnnStr)
        let cnt = query { for _ in ctx.Feeds do count }
        cnt |> should be (equal 1)

        let cnt = query { for _ in ctx.Chains do count }
        cnt |> should be (equal 1)

        let ch = query { for ch in ctx.Chains do 
                         select ch 
                         exactlyOne }

        ch.Expanded.Value |> should be (equal <| DateTime(2014,5,8))

    [<TestCase>]
    let ``Startup with one chain`` () =
        let dt = DateTime(2014,5,8)
                
        initDb ()

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

            m  "MSG: @%A %s" state (fail.ToString())
        )

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Connect" x.Connect (State Initialized)
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Close" x.Close (State Closed)
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding

        checkData ()
//
//        clearDb ()
//
//        checkNoData()