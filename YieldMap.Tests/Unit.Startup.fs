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

    open YieldMap.Tools.Logging

    let logger = LogFactory.create "StartupTest"
    
    [<TestCase>]
    let ``Simple workflow`` () =
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
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Connect" x.Connect (State Initialized)
        command "Reload" (fun () -> x.Reload true) (State Initialized)
        command "Close" x.Close (State Closed)
        command "Close" x.Close NotResponding
        command "Connect" x.Connect NotResponding

    [<TestCase>]
    let ``Parallel workflow`` () =
        let dt = DateTime(2014,3,4)
                
        let zzz = {
            Factory = MockFactory()
            TodayFix = dt
            Loader = MockChainMeta dt
            Calendar = MockCalendar dt
        }

        let x = Startup zzz

        let currentState = ref Started
        Notifier.notification |> Observable.add (fun (state, fail, severity) -> 
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

            let! res = command "Reload" (fun () -> x.Reload true)
            res |> should be (equal (State Initialized))

            let! res = command "Connect" x.Connect
            res |> should be (equal (State Initialized))

            let! res = command "Reload" (fun () -> x.Reload true)
            res |> should be (equal (State Initialized))

            do! Async.Sleep 5000

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

            let! res = command2 "Reload" (fun () -> x.Reload true)
            res |> should be (equal (State Initialized))

            // main flow sleeps 5 seconds after reload, so here I am
            let! res = command2 "Connect" x.Connect 
            res |> should be (equal (State Initialized))
        }
        
        let exns = 
            [ Async.Catch mainFlow; Async.Catch additionalFlow] 
            |> Async.Parallel 
            |> Async.RunSynchronously
            |> Array.choose (function Choice2Of2 e -> Some e | _ -> None)
        
        if not <| Array.isEmpty exns then raise <| AggregateException exns