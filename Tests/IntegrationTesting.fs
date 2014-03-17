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
        open YieldMap.Tools.Logging
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

                data |> Array.length |> should equal cnt
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

                use subscription = new EikonSubscription(!eikon, "IDN")
                let s =  subscription :> Subscription
                let answer = s.Fields (["RUB="; "GAZP.MM"], None) |> Async.RunSynchronously
                logger.Info <| sprintf "Got answer %A" answer
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()


        let snapshot ricFields  =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            let q = OuterEikonFactory(!eikon) :> Loader
            try

                try
                    let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                    ans |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"

                use subscription = new EikonSubscription(!eikon, "IDN")
                let s =  subscription :> Subscription
                let answer = s.Snapshot (ricFields, Some 100000) |> Async.RunSynchronously
                logger.Info <| sprintf "Got answer %A" answer
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()



        [<Test>]
        let ``snapshot-test`` () =
            let ricFields = [("RUB=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK"])] |> Map.ofList
            snapshot ricFields

        [<Test>]
        let ``snapshot-test-1`` () =
            let ricFields = [("XXX", ["BID"; "ASK"]); ("GAZP.MM", ["BID12"; "ASK33"])] |> Map.ofList
            snapshot ricFields

        [<Test>]
        let ``snapshot-test-2`` () =
            let ricFields = [("XXX", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK33"])] |> Map.ofList
            snapshot ricFields

        [<Test>]
        let ``snapshot-test-3`` () =
            let ricFields = [("EUR=", ["BID"; "ASK"]); ("GAZP.MM", ["BID"; "ASK33"])] |> Map.ofList
            snapshot ricFields