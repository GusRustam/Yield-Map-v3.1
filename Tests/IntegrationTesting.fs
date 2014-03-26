namespace YieldMap.Tests.Common

    open System
    open System.IO
    open System.Xml

    open NUnit.Framework
    open FsUnit

    module IntegrationTesting = 
        open System
        open System.IO
        open System.Xml

        open EikonDesktopDataAPI

        open YieldMap.Requests.Answers
        open YieldMap.Requests
        open YieldMap.Loading.LiveQuotes
        open YieldMap.Loading.SdkFactory
        open YieldMap.MetaTables
        open YieldMap.Logging
        open YieldMap.Tools

        open NUnit.Framework

        let logger = LogFactory.create "Dex2Tests"
       
        [<Test>]
        let ``connection`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            try
                let q = OuterEikonFactory(!eikon) :> Loader
                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

        [<Test>]
        let ``retrieve-real-data`` () = 
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            try
                let q = OuterEikonFactory(!eikon) :> Loader
                try
                    Async.RunSynchronously (Dex2Tests.test q "0#RUCORP=MM", int <| TimeSpan.FromMinutes(1.0).TotalMilliseconds) |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

                
        [<TestCase(0, 0, 126)>]
        [<TestCase(1, 0, 87)>]
        [<TestCase(0, 1, 39)>]
        [<TestCase(1, 1, 0)>]
        let ``chains-in-parallel`` (t1 : int, t2 : int, cnt : int) =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try

                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"
                
                let toSome t = if t <= 0 then None else Some t

                logger.Trace <| sprintf "Testing chain timeout %A -> %A -> %d" (toSome t1) (toSome t2) cnt

                let chain name timeout = Dex2Tests.getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = timeout }
                
                let tasks = [ chain "0#RUTSY=MM" (toSome t1); chain "0#RUSOVB=MM" (toSome t2) ]
                
                let data = tasks |> Async.Parallel |> Async.RunSynchronously |> Array.collect id

                printfn "%d : %A" (Array.length data) data

                data |> Array.length |> should (equalWithin 5) cnt
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

        [<Test>]
        let ``fields-test`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try

                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"

                use subscription = new EikonSubscription(!eikon, "IDN", QuoteMode.OnUpdate)
                let s =  subscription :> Subscription
                let answer = s.Fields (["RUB="; "GAZP.MM"], None) |> Async.RunSynchronously
                logger.Info <| sprintf "Got answer %A" answer
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

        let snapshot ricFields eikon =
            let counts (wut:RicFieldValue) = 
                let rec cntRics rics fieldsValues = function
                    | (ric, fieldValue) :: rest -> 
                        let addon = fieldValue |> Map.toList |> List.length
                        rest |> cntRics (rics+1) (fieldsValues + addon)
                    | [] -> rics, fieldsValues
                wut |> Map.toList |> cntRics 0 0

            use subscription = new EikonSubscription(!eikon, "IDN", QuoteMode.OnUpdate)
            let s = subscription :> Subscription
            let answer = s.Snapshot (ricFields, Some 100000) |> Async.RunSynchronously
            logger.Info <| sprintf "Got answer %A" answer

            match answer with Succeed wut -> counts wut | _ -> 0, 0

        [<Test>]
        let ``snapshot-tests`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try
                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"

                let ricFields = [("RUB=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                snapshot ricFields eikon |> should equal (2, 4)
                let ricFields = [("XXX", ["BID"; "ASK"]); ("GAZP.MM", ["BID12"; "ASK33"])] |> Map.ofList
                snapshot ricFields eikon |> should equal (1, 0)
                let ricFields = [("XXX", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK33"])] |> Map.ofList
                snapshot ricFields eikon |> should equal (1, 1)
                let ricFields = [("EUR=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK33"])] |> Map.ofList
                snapshot ricFields eikon |> should equal (2, 3)
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

        let considerIt eikon mode ricFields = 
            use bebebe = new EikonSubscription(!eikon, "IDN", mode)
            let qoqoqo = bebebe :> Subscription

            qoqoqo.Add ricFields
            qoqoqo.OnQuotes |> Observable.add (fun q -> 
                logger.Info <| sprintf "Got quotes %A" q
                try logger.Info <| sprintf "GAZPROM BID IS %s" q.["GAZP.MM"].["BID"]
                with _ -> logger.Info "NO GAZP BID")

            let always x = (fun _ -> x)
            let count = ref 0
            qoqoqo.OnQuotes |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun q -> logger.Info <| sprintf "Update #%d" q; count := q)
                    
            qoqoqo.Start ()
            Async.Sleep 10000 |> Async.RunSynchronously
            qoqoqo.Stop ()    
            !count

        [<Test>]
        let ``realtime-quotes`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try
                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"

                // test

                logger.Info "1"
                let rf = [("RUB=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon QuoteMode.OnUpdate rf |> should be (greaterThan 1)

                logger.Info "2"
                let rf = [("RUB=", ["BID"; "ASK"]); ("GxAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon QuoteMode.OnUpdate rf |> should be (greaterThan 1)

                logger.Info "3"
                let rf = [("RxUB=", ["BID"; "ASK"]); ("GxAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon QuoteMode.OnUpdate rf |> should be (equal 0)

            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()

        [<Test>]
        let ``realtime-quotes-2`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try
                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"

                // test

                logger.Info "4"
                let rf = [("RUB=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon (QuoteMode.OnTime 4) rf |> should be (greaterThan 1)

                logger.Info "5"
                let rf = [("RUB=", ["BID"; "ASK"]); ("GxAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon (QuoteMode.OnTime 4) rf |> should be (greaterThan 1)

                logger.Info "6"
                let rf = [("RxUB=", ["BID"; "ASK"]); ("GxAZP.MM", ["BID"; "ASK"])] |> Map.ofList
                considerIt eikon (QuoteMode.OnTime 4) rf |> should be (equal 3)

            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()