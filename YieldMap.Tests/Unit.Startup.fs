namespace YieldMap.Tests.Unit

open System
open System.IO

open NUnit.Framework
open FsUnit

//module StartupTests =
//    open YieldMap.Loader.Calendar
//    open YieldMap.Loader.MetaChains
//    open YieldMap.Loader.SdkFactory
//
//    open YieldMap.Core.Application
//
//    open YieldMap.Tools.Logging
//    let logger = LogFactory.create "StartupTests"
//
//    // TEST ALL POSSIBLE SITUATIONS INCLUDING FAILURES DURING DATALOAD
//    // FOR THIS CASE I WILL HAVE TO MOCK MockChainMeta ???? Yep. See http://stackoverflow.com/a/2462672/1554463
//
//    // Also test tomorrow
//
//    // ANOZER KWESCHEN: SHOULD I BACKUP DA DATABASE? I REALLY DUNNO!!!
//    // I guess I should implement backup via import / export!
//
//    [<Test>]
//    let ``1. Simple test on app startup`` () =
//        let dt = DateTime(2014,3,4)
//                
//        let f = MockFactory()
//        let c = MockCalendar dt
//        let m = MockChainMeta dt
//
//        let q = Startup(f, c, m)
//
//        q.StateChanged |> Observable.add (fun x -> logger.InfoF "State changed to %A" x)
//        q.Notification |> Observable.add (fun n -> 
//            match n with
//            | f, s when s = Severity.Evil -> logger.ErrorF "Notification %A" f
//            | f, s when s = Severity.Warn -> logger.WarnF "Notification %A" f
//            | f, s -> logger.InfoF "Notification %A" f
//        )
//
//        let state = q.Initialze() 
//        logger.InfoF "After initialzie state is %A" state 
//
//        state |> should be (equal AppState.Connected)
//
//        let state = q.Initialze() 
//        logger.InfoF "After second initialzie state is %A" state 
//        state |> should be (equal AppState.Connected)
//
//        let state = q.Reload() |> Async.RunSynchronously
//        logger.InfoF "After load state is %A" state 
//        state |> should be (equal AppState.Initialized)


module StartupTest = 
    open YieldMap.Core.Application.AnotherStartup
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "StartupTest"
    
    [<TestCase>]
    let ``Simple workflow`` () =
        let x = Boxing()

        x.Notification |> Observable.add (fun (state, msg) -> logger.TraceF "MSG: @%A %s" state msg)
        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)

        logger.Info " ===> Connect "
        let res = x.Connect |> Async.RunSynchronously  
        logger.InfoF " <=== Connect : %s " (res.ToString())
        res |> should be (equal <| State Connected)

        logger.Info " ===> Reload "
        let res = x.Reload |> Async.RunSynchronously 
        logger.InfoF " <=== Reload : %s " (res.ToString())
        res |> should be (equal <| State Initialized)

        logger.Info " ===> Connect "
        let res = x.Connect |> Async.RunSynchronously
        logger.InfoF " <=== Connect : %s " (res.ToString())
        res |> should be (equal <| State Initialized)

        logger.Info " ===> Reload "
        let res = x.Reload |> Async.RunSynchronously
        logger.InfoF " <=== Reload : %s " (res.ToString())
        res |> should be (equal <| State Initialized)

        logger.Info " ===> Close "
        let res = x.Close |> Async.RunSynchronously
        logger.InfoF " <=== Close : %s " (res.ToString())
        res |> should be (equal <| State Closed)

        logger.Info " ===> Close "
        let res = x.Close |> Async.RunSynchronously
        logger.InfoF " <=== Close : %s " (res.ToString())
        res |> should be (equal <| NotResponding)

        logger.Info " ===> Connect "
        let res = x.Connect |> Async.RunSynchronously
        logger.InfoF " <=== Connect : %s " (res.ToString())
        res  |> should be (equal <| NotResponding)
