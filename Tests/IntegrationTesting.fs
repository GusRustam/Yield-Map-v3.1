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
                    Async.RunSynchronously (Dex2Tests.test q "0#RUCORP=MM", 30000) |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()
