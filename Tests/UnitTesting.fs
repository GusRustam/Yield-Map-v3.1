namespace YieldMap.Tests.Common

    open System
    open System.IO
    open System.Xml

    open NUnit.Framework
    open FsUnit

    module DataTests = 
        open YieldMap.Data
        open YieldMap.Data.Answers
        open YieldMap.Data.Requests
        open YieldMap.Data.Loading
        open YieldMap.Data.MetaTables
        open YieldMap.Tools

        [<Test>]
        let ``connection`` () = 
            let q = MockLoader() :> MetaLoader
            let ans =  Dex2Tests.connect q |> Async.RunSynchronously
            ans |> should be True

        [<Test; Timeout(1000000); MaxTime(1000000)>]
        let ``retrieve-mock-data`` () = 
            try
                let dt = DateTime(2014,3,4)
                let q = MockLoader(Some dt) :> MetaLoader

                Logging.globalThreshold := Logging.LoggingLevel.Debug

                Dex2Tests.test q "0#RUCORP=MM" |> Async.RunSynchronously |> should be True
            with e -> 
                logger.Error <| sprintf "Failed %s" (e.ToString())
                Assert.Fail()
