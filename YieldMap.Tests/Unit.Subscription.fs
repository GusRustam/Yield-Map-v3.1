namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module MockSubscriptionTest = 
    open YieldMap.Tools.Logging
    open YieldMap.Loader.LiveQuotes

    let logger = LogFactory.create "UnitTests.LiveQuotesTest"

    [<Test>]
    let ``I receive quotes I'm subscribed`` () =
        logger.InfoF "I receive quotes I subscribed"
        let slots = seq {
            yield { Interval = 1.0; Items = [{Ric="YYY";Field="BID";Value="11"}] }
            yield { Interval = 2.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
        }
        let generator = SeqGenerator (slots, false)
        use data = new MockSubscription(generator)
        let subscription = data :> Subscription
        let count = ref 0
        subscription.Add ([("XXX",["BID"])] |> Map.ofList)
        subscription.OnQuotes |> Observable.add (fun q -> 
            logger.InfoF "Got quote %A" q
            count := !count + 1)
        subscription.Start ()
        Async.Sleep 7000 |> Async.RunSynchronously
        !count |> should equal 2

    [<Test>]
    let ``I don't receive quotes I'm not subscribed`` () =
        logger.InfoF "I don't receive quotes I'm not subscribed"
        let slots = seq {
            yield { Interval = 1.0; Items = [{Ric="XXX";Field="ASK";Value="11"}] }
            yield { Interval = 2.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
        }
        let generator = SeqGenerator (slots, false)
        use data = new MockSubscription(generator)
        let subscription = data :> Subscription
        let count = ref 0
        subscription.Add ([("XXX",["BID"])] |> Map.ofList)
        subscription.OnQuotes |> Observable.add (fun q -> 
            logger.InfoF "Got quote %A" q
            count := !count + 1)
        subscription.Start ()
        Async.Sleep 7000 |> Async.RunSynchronously
        !count |> should equal 2

    [<Test>]
    let ``I stop receiveing quotes I've unsubscribed`` () =
        logger.InfoF "I stop receive quotes I've unsubscribed"
        let slots = seq {
            yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
            yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
        }
        let generator = SeqGenerator (slots, false)
        use data = new MockSubscription(generator)
        let subscription = data :> Subscription
        let count = ref 0
        subscription.Add ([("XXX",["BID"])] |> Map.ofList)
        subscription.OnQuotes |> Observable.add (fun q -> 
            logger.InfoF "Got quote %A" q
            count := !count + 1)

        subscription.Start ()
        Async.Sleep 6000 |> Async.RunSynchronously
        subscription.Remove ["XXX"]
        Async.Sleep 6000 |> Async.RunSynchronously

        !count |> should equal 1

    [<Test>]
    let ``I begin receiveing quotes I've subscribed to`` () =
        logger.InfoF "I begin receiveing quotes I've subscribed to"
        let slots = seq {
            yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
            yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
        }
        let generator = SeqGenerator (slots, false)
        use data = new MockSubscription(generator)
        let subscription = data :> Subscription
        let count = ref 0
        subscription.Add ([("XXX",["BID"])] |> Map.ofList)
        subscription.OnQuotes |> Observable.add (fun q -> 
            logger.InfoF "Got quote %A" q
            count := !count + 1)

        subscription.Start ()
        Async.Sleep 6000 |> Async.RunSynchronously
        subscription.Add ([("YYY",["ASK"])] |> Map.ofList)
        Async.Sleep 5000 |> Async.RunSynchronously

        !count |> should equal 3

    [<Test>]
    let ``I receive snapshots according to recent quotes`` () =
        logger.InfoF "I receive snapshots according to recent quotes"
        let slots = seq {
            yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
            yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
            yield { Interval = 3.0; Items = [{Ric="YYY";Field="ASK";Value="22"}] }
        }

        let generator = SeqGenerator (slots, false)
        use data = new MockSubscription(generator)
        let subscription = data :> Subscription

        subscription.Start ()

        let x = subscription.Snapshot (([("XXX",["BID"])] |> Map.ofList), None) |> Async.RunSynchronously
        match x with Succeed rfv -> counts rfv |> should equal (0,0) | _ -> Assert.Fail()

        let x = subscription.Snapshot (([("XXX",["BID"]); ("ZZZ",["QQQ"])] |> Map.ofList), None) |> Async.RunSynchronously
        match x with Succeed rfv -> counts rfv |> should equal (0,0) | _ -> Assert.Fail()

        Async.Sleep 5200 |> Async.RunSynchronously

        let x = subscription.Snapshot (([("XXX",["BID"])] |> Map.ofList), None) |> Async.RunSynchronously
        match x with Succeed rfv -> counts rfv |> should equal (1,1) | _ -> Assert.Fail()

        Async.Sleep 3500 |> Async.RunSynchronously

        let x = subscription.Snapshot (([("XXX",["BID"]); ("YYY",["ASK"])] |> Map.ofList), None) |> Async.RunSynchronously
        logger.InfoF "Got snapshot %A" x
        match x with 
        | Succeed rfv -> 
            counts rfv |> should equal (2,2) 
            rfv.["YYY"].["ASK"] |> should equal "22"
        | _ -> Assert.Fail()

        let x = subscription.Snapshot (([("YYY",["ASK"])] |> Map.ofList), None) |> Async.RunSynchronously
        match x with 
        | Succeed rfv -> counts rfv |> should equal (1,1) 
        | _ -> Assert.Fail()
