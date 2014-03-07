namespace YieldMap.Tests.Common

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
        open YieldMap.Tools.Logging

        let logger = LogFactory.create "Dex2Tests"

        [<Test>]
        let ``connection`` () = 
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
                logger.Error <| sprintf "Failed %s" (e.ToString())
                Assert.Fail()
        
        [<TestCase(0, 0, 126)>]
        [<TestCase(1, 0, 87)>]
        [<TestCase(0, 1, 39)>]
        [<TestCase(1, 1, 0)>]
        let ``chains-in-parallel`` (t1 : int, t2 : int, cnt : int) =
            let toSome t = if t <= 0 then None else Some t

            logger.Trace <| sprintf "Testing chain timeout %A -> %A -> %d" (toSome t1) (toSome t2) cnt

            let dt = DateTime(2014,3,4)
            let q = MockOnlyFactory(dt) :> Loader
            let chain name timeout = Dex2Tests.getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = timeout }
                
            let tasks = [ chain "0#RUTSY=MM" (toSome t1); chain "0#RUSOVB=MM" (toSome t2) ]
                
            let data = tasks |> Async.Parallel |> Async.RunSynchronously |> Array.collect id

            printfn "%d : %A" (Array.length data) data

            data |> Array.length |> should equal cnt