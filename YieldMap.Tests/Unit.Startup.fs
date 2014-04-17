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
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory

    open YieldMap.Core.Application.AnotherStartup
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "StartupTest"
    
    [<TestCase>]
    let ``Simple workflow`` () =
        let dt = DateTime(2014,3,4)
                
        let f = MockFactory()
        let c = MockCalendar dt
        let m = MockChainMeta dt

        let x = Startup(f, c, m)

        x.StateChanged |> Observable.add (fun state -> logger.InfoF " => %A" state)
        x.Notification |> Observable.add (fun (state, fail, severity) -> 
            match severity with
            | Evil -> logger.ErrorF "MSG: @%A %s" state (fail.ToString())
            | Warn -> logger.TraceF "MSG: @%A %s" state (fail.ToString())
            | Note -> logger.InfoF "MSG: @%A %s" state (fail.ToString())
        )

        let command cmd func state = 
            logger.InfoF "===> %s " cmd
            let res = func () |> Async.RunSynchronously  
            logger.InfoF " <=== %s : %s " cmd (res.ToString())
            res |> should be (equal state)

        command "Connect" x.Connect (State Connected)
        command "Reload" x.Reload (State Initialized)
        command "Connect" x.Connect (State Initialized)
        command "Reload" x.Reload (State Initialized)
        command "Close" x.Close (State Closed)
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding

    [<TestCase>]
    let ``Parallel workflow`` () =
        let dt = DateTime(2014,3,4)
                
        let f = MockFactory()
        let c = MockCalendar dt
        let m = MockChainMeta dt

        let x = Startup(f, c, m)

        let currentState = ref Started
        x.Notification |> Observable.add (fun (state, fail, severity) -> 
            match severity with
            | Evil -> logger.ErrorF "MSG: @%A %s" state (fail.ToString())
            | Warn -> logger.TraceF "MSG: @%A %s" state (fail.ToString())
            | Note -> logger.InfoF "MSG: @%A %s" state (fail.ToString())
        )

        x.StateChanged |> Observable.add (fun state -> logger.InfoF "state -> %A" state; currentState := state)

        let comm id cmd func = async {
            logger.InfoF "%d) ===> %s " id cmd
            let! res = func ()
            logger.InfoF "%d) <=== %s : %s " id  cmd (res.ToString())
            return res
        }
        
        let command = comm 1
        let command2 = comm 2

        let mainFlow = async {
            let! res = command "Connect" x.Connect 
            res |> should be (equal (State Connected))

            let! res = command "Reload" x.Reload
            res |> should be (equal (State Initialized))

            let! res = command "Connect" x.Connect
            res |> should be (equal (State Initialized))

            let! res = command "Reload" x.Reload
            res |> should be (equal (State Initialized))

            let! res = command "Close" x.Close
            res |> should be (equal (State Closed))

            let! res = command "Close" x.Close 
            res |> should be (equal NotResponding)

            let! res = command "Connect" x.Connect 
            res |> should be (equal NotResponding)
        }

        let additionalFlow = async {
            do! Async.Sleep 100

            let! res = command2 "Connect" x.Connect 
            res |> should be (equal (State Connected))

            let! res = command2 "Reload" x.Reload
            res |> should be (equal NotResponding)
        }
        
        let exns = 
            [ Async.Catch mainFlow; Async.Catch additionalFlow] 
            |> Async.Parallel 
            |> Async.RunSynchronously
            |> Array.choose (function Choice2Of2 e -> Some e | _ -> None)
        
        if not <| Array.isEmpty exns then raise <| AggregateException exns