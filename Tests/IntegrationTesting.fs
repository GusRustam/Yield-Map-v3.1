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

        open YieldMap.Data.Answers
        open YieldMap.Data.Requests
        open YieldMap.Data.Loading
        open YieldMap.Data.MetaTables
        open YieldMap.Data
        open YieldMap.Tools

        open NUnit.Framework

        [<Test>]
        let ``connection`` () =
            let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
            try
                let q = OuterLoader(!eikon) :> MetaLoader
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
                let q = OuterLoader(!eikon) :> MetaLoader
                try
                    Async.RunSynchronously (Dex2Tests.test q "0#RUCORP=MM", 30000) |> should be True
                with :? TimeoutException -> 
                    logger.Error "...timeout"
                    Assert.Fail "Timeout"
            finally
                Ole32.killComObject eikon
                Ole32.CoUninitialize()
