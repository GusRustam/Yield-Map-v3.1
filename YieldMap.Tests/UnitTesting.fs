namespace YieldMap.Tests.Unit

    open System
    open System.IO
    open System.Xml

    open YieldMap.Tests.Common

    open NUnit.Framework
    open FsUnit

    module MetaChainsTests = 
        open YieldMap.Loader.Requests
        open YieldMap.Loader.Requests.Answers
        open YieldMap.Loader.Loading
        open YieldMap.Loader.Requests
        open YieldMap.Loader.Loading.SdkFactory
        open YieldMap.Loader.MetaTables
        open YieldMap.Loader.Calendar
        
        open YieldMap.Tools.Logging

        let logger = LogFactory.create "Dex2Tests"

        [<Test>]
        let ``Mock connection establishes`` () = 
            let q = MockOnlyFactory() :> Loader
            let ans =  Dex2Tests.connect q |> Async.RunSynchronously
            ans |> should be True

        [<Test>]
        let ``Requested chain is recieved`` () = 
            try
                let dt = DateTime(2014,3,4)
                let q = MockOnlyFactory(dt) :> Loader

                globalThreshold := LoggingLevel.Debug

                Dex2Tests.test q "0#RUTSY=MM" |> Async.RunSynchronously |> should be True
            with e -> 
                logger.ErrorF "Failed %s" (e.ToString())
                Assert.Fail()
        
        [<TestCase(0, 0, 126)>]
        [<TestCase(1, 0, 87)>]
        [<TestCase(0, 1, 39)>]
        [<TestCase(1, 1, 0)>]
        let ``Chains come up in parallel and if some fails, the other don't`` (t1 : int, t2 : int, cnt : int) =
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
        let ``Tomorrow event happens in mock calendar`` () =
            let always x = fun _ -> x
            let count = ref 0

            let clndr = Calendar.MockCalendar(DateTime(2010, 1, 31, 23, 59, 55)) :> Calendar
            
            clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
            clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

            Async.AwaitEvent clndr.NewDay |> Async.Ignore |> Async.Start
            Async.Sleep(10000) |> Async.RunSynchronously
            !count |> should equal 1

    module MockSubscriptionTest = 
        open YieldMap.Tools.Logging
        open YieldMap.Loader.LiveQuotes

        let logger = LogFactory.create "LiveQuotesTest"

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

    module TestWebServer = 
        open System.Net
        open System.Text

        open YieldMap.Tools.Logging
        open YieldMap.Loader.WebServer
        open YieldMap.Tools.Aux

        let logger = LogFactory.create "TestWebServer"

        [<Test>]
        let ``Web server starts, responds and stops`` () = 
            ApiServer.start ()
            logger.InfoF "Server started"
            Async.Sleep(3000) |> Async.RunSynchronously

            use wb = new WebClient()
            let res = wb.DownloadString(ApiServer.host)
            logger.InfoF "Got answer %A" res
            res.Substring(0,3) |> should equal "ERR"

            ApiServer.stop ()
            logger.InfoF "Server stopping"
            Async.Sleep(3000) |> Async.RunSynchronously
            logger.InfoF "Server must be stopped"

            let req = wb.AsyncDownloadString <| Uri ApiServer.host
            try
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                Assert.Fail()
            with :? TimeoutException ->
                logger.InfoF "Done"

        [<Test>]
        let ``Web server accepts quotes`` () = 
            ApiServer.start ()
            logger.InfoF "Seems 2B started"
            Async.Sleep(5000) |> Async.RunSynchronously

            logger.InfoF "Sending request"
            use wb = new WebClient()

            let q = ApiQuote.create "XXX" "FLD" "12"
            let z = ApiQuotes.create [|q|]
            let enc = ApiQuotes.pack z

            let resp = wb.UploadData(ApiServer.host + "quote", enc)
            let response = Encoding.ASCII.GetString(resp)

            logger.InfoF "Response is %s" response
            response |> should equal "OK"

            ApiServer.stop ()

    module TestApiQuotes =
        open System.Net
        open System.Text
        open System.Threading

        open YieldMap.Tools.Logging
        open YieldMap.Loader.LiveQuotes
        open YieldMap.Tools.Aux
        open YieldMap.Loader.WebServer

        let logger = LogFactory.create "TestApiQuotes"

        [<Test>]
        let ``I recieve quotes I sent`` () = 
            let apiQuotes = ApiSubscription() :> Subscription

            let slots = [|
                ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "12"|]
                ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "13"|]
                ApiQuotes.create [|ApiQuote.create "XXX" "BID" "95"|]
                ApiQuotes.create [|
                    ApiQuote.create "YYY" "BID" "10"
                    ApiQuote.create "YYY" "ASK" "15"
                    ApiQuote.create "XXX" "ASK" "105"
                |]
            |]

            let count = ref 0

            let subscription = [
                ("YYY", ["BID"; "ASK"])
                ("XXX", ["BID"; "ASK"])
            ]

            subscription |> Map.ofList |> apiQuotes.Add 
            apiQuotes.Start()
            apiQuotes.OnQuotes 
            |> Observable.scan (fun (count, item) evt -> (count+1, evt)) (-1, Map.empty)
            |> Observable.add (fun (i, rfv) -> 
                count := i+1
                logger.InfoF "Got quotes %A" rfv
                try rfv |> should equal (ApiQuotes.toRfv slots.[i])
                with e -> logger.ErrorEx "Failed" e
                logger.InfoF "Ok"
            )

            use wb = new WebClient()

            slots |> Array.iter (fun slot -> 
                logger.InfoF "To send quotes %A" slot
                let enc = ApiQuotes.pack slot
                let resp = wb.UploadData(ApiServer.host + "quote", enc)
                let response = Encoding.ASCII.GetString(resp)
                response |> should equal "OK"
                Async.Sleep(1000) |> Async.RunSynchronously
            )

            !count |> should equal (Array.length slots)
            apiQuotes.Stop()

    module DbTests =
        open YieldMap.Tools.Logging
        open YieldMap.Database
        let logger = LogFactory.create "TestApiQuotes"

        [<Test>]
        let ``Read something from Db`` () = 
            use ctx = new MainEntities()
            let q = query {
                for x in ctx.RefChains do 
                select x 
                count
            }
            logger.InfoF "Da count is %d" q
            let c = RefChain()
            c.Name <- "Hello"
            let c = ctx.RefChains.Add c
            ctx.SaveChanges() |> ignore

            logger.InfoF "Now c is %A" c
            let q = query {
                for x in ctx.RefChains do 
                select x 
                count
            }
            logger.InfoF "Da count is now %d" q

            let c = ctx.RefChains.Remove (c)
            ctx.SaveChanges() |> ignore
            logger.InfoF "And now c is %A" c
            let q = query {
                for x in ctx.RefChains do 
                select x 
                count
            }
            logger.InfoF "Da count is now %d" q
