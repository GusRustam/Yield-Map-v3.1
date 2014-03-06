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

        [<Test>]
        let ``chains-in-parallel`` () =
            try
                let dt = DateTime(2014,3,4)
                let q = MockOnlyFactory(dt) :> Loader
                let chain name = Dex2Tests.getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = None }
                let tasks = [ chain "0#RUTSY=MM"; chain "0#RUSOVB=MM" ]

                ()
            with e -> 
                logger.Error <| sprintf "Failed %s" (e.ToString())
                Assert.Fail()
