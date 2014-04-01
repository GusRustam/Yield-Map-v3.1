﻿namespace YieldMap.Tests.Common

    open System
    open System.IO
    open System.Xml

    open NUnit.Framework
    open FsUnit

    module DataTests = 
        open YieldMap.Requests
        open YieldMap.Requests.Answers
        open YieldMap.Loading
        open YieldMap.Loading.SdkFactory
        open YieldMap.MetaTables
        open YieldMap.Logging
        open YieldMap.Calendar

        let logger = LogFactory.create "Dex2Tests"

        [<Test>]
        let ``Mock connection establishes`` () = 
            let q = MockOnlyFactory() :> Loader
            let ans =  Dex2Tests.connect q |> Async.RunSynchronously
            ans |> should be True

        [<Test>]
        let ``retrieve-mock-data`` () = 
            try
                let dt = DateTime(2014,3,4)
                let q = MockOnlyFactory(dt) :> Loader

                globalThreshold := LoggingLevel.Debug

                Dex2Tests.test q "0#RUTSY=MM" |> Async.RunSynchronously |> should be True // Why it fails, I don't know ((
            with e -> 
                logger.ErrorF "Failed %s" (e.ToString())
                Assert.Fail()
        
        [<TestCase(0, 0, 126)>]
        [<TestCase(1, 0, 87)>]
        [<TestCase(0, 1, 39)>]
        [<TestCase(1, 1, 0)>]
        let ``chains-in-parallel`` (t1 : int, t2 : int, cnt : int) =
            let toSome t = if t <= 0 then None else Some t

            logger.TraceF "Testing chain timeout %A -> %A -> %d" (toSome t1) (toSome t2) cnt

            let dt = DateTime(2014,3,4)
            let q = MockOnlyFactory(dt) :> Loader
            let chain name timeout = Dex2Tests.getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = timeout }
                
            let tasks = [ chain "0#RUTSY=MM" (toSome t1); chain "0#RUSOVB=MM" (toSome t2) ]
                
            let data = tasks |> Async.Parallel |> Async.RunSynchronously |> Array.collect id

            printfn "%d : %A" (Array.length data) data

            data |> Array.length |> should equal cnt

        [<Test>]
        let ``tomorrow-test`` () =
            let always x = fun _ -> x
            let count = ref 0

            let clndr = Calendar.MockCalendar(DateTime(2010, 1, 31, 23, 59, 55)) :> Calendar
            
            clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
            clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

            Async.AwaitEvent clndr.NewDay |> Async.Ignore |> Async.Start
            Async.Sleep(10000) |> Async.RunSynchronously
            !count |> should equal 1

    module LiveQuotesTest = 
        open YieldMap.Logging
        open YieldMap.Loading.LiveQuotes

        let logger = LogFactory.create "LiveQuotesTest"

        [<Test>]
        let ``I receive quotes I'm subscribed`` () =
            logger.Info "I receive quotes I subscribed"
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
            logger.Info "I don't receive quotes I'm not subscribed"
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
            logger.Info "I stop receive quotes I've unsubscribed"
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
            logger.Info "I begin receiveing quotes I've subscribed to"
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
            logger.Info "I receive snapshots according to recent quotes"
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

    module TestWebServer = 
        open System.Net
        open System.Text

        open YieldMap.Logging
        open YieldMap.WebServer
        open YieldMap.Tools

        let logger = LogFactory.create "TestWebServer"

        [<Test>]
        let ``Web server starts, responds and stops`` () = 
            ApiServer.start ()
            logger.Info "Server started"
            Async.Sleep(3000) |> Async.RunSynchronously

            use wb = new WebClient()
            let res = wb.DownloadString(ApiServer.host)
            logger.InfoF "Got answer %A" res
            res.Substring(0,3) |> should equal "ERR"

            ApiServer.stop ()
            logger.Info "Server stopping"
            Async.Sleep(3000) |> Async.RunSynchronously
            logger.Info "Server must be stopped"

            let req = wb.AsyncDownloadString <| Uri ApiServer.host
            try
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                Assert.Fail()
            with :? TimeoutException ->
                logger.Info "Done"

        [<Test>]
        let ``Web server accepts quotes`` () = 
            ApiServer.start ()
            logger.Info "Seems 2B started"
            Async.Sleep(5000) |> Async.RunSynchronously

            logger.Info "Sending request"
            use wb = new WebClient()

            let q = ApiQuote.create "XXX" "FLD" "12"
            let z = ApiQuotes.create [|q|]
            let enc = ApiQuotes.pack z

            let resp = wb.UploadData(ApiServer.host + "quote", enc)
            let response = Encoding.ASCII.GetString(resp)

            logger.InfoF "Response is %s" response
            response |> should equal "OK"

            ApiServer.stop ()