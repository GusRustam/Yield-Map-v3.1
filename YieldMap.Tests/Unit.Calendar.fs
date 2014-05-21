namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module ``Calendar`` = 
    open YieldMap.Loader.Calendar
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "UnitTests.Calendar"

    [<Test>]
    let ``Tomorrow event happens in mock calendar`` () =
        let always x = fun _ -> x
        let count = ref 0

        let clndr = Calendar.MockCalendar(DateTime(2010, 1, 31, 23, 59, 55)) :> Calendar
            
        clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
        clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

        Async.AwaitEvent clndr.NewDay |> Async.Ignore |> Async.Start
        Async.Sleep(10000) |> Async.RunSynchronously
        !count |> should equal 1

    [<Test>]
    let ``Changeable date calendar - tomorrow happens`` () =
        let always x = fun _ -> x
        let count = ref 0

        use d = new Calendar.UpdateableCalendar(DateTime(2010, 1, 31, 23, 59, 55))
        let clndr = d :> Calendar
            
        clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
        clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

        Async.AwaitEvent clndr.NewDay |> Async.Ignore |> Async.Start
        Async.Sleep(10000) |> Async.RunSynchronously
        !count |> should equal 1

    [<Test>]
    let ``Changeable date calendar - setting and resetting date`` () =
        let always x = fun _ -> x
        let count = ref 0

        let start = DateTime(2010, 1, 31, 21, 50, 00)
        use d = new Calendar.UpdateableCalendar(start)
        let clndr = d :> Calendar
            
        clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
        clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

        Async.Sleep(4000) |> Async.RunSynchronously // sleep 3 seconds
        let delta = (clndr.Now - start).Seconds
        delta |> should be (lessThan 5) // no more than 5 seconds must pass
        
        d.SetTime start  // reset datetime and try again
        Async.Sleep(4000) |> Async.RunSynchronously // sleep 3 seconds
        let delta = (clndr.Now - start).Seconds
        delta |> should be (lessThan 5) // no more than 5 seconds must pass again

        let start = DateTime(2010, 1, 31, 23, 59, 50)
        d.SetTime start  // reset datetime
        Async.Sleep(5000) |> Async.RunSynchronously // sleep 5 seconds
        !count |> should equal 0 // no tomorrow should happen in 5 seconds

        let start = DateTime(2010, 1, 31, 23, 59, 59) // one second until tom
        d.SetTime start  // reset datetime
        Async.Sleep(3000) |> Async.RunSynchronously // sleep 3 seconds
        !count |> should equal 1 //and now tom is here