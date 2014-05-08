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

    open YieldMap.Tools.Logging

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

    [<TestCase>]
    let ``Startup with one chain`` () =
        let dt = DateTime(2014,5,8)
                
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