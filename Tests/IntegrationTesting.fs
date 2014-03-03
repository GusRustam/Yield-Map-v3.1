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

        open NUnit.Framework

        [<Test>]
        let ``connection`` () =
            let eikon = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI 
            let q = OuterLoader(eikon) :> MetaLoader
            try
                let ans =  Async.RunSynchronously(Dex2Tests.connect q, 10000)
                ans |> should be True
            with :? TimeoutException -> 
                logger.Error "...timeout"
                Assert.Fail "Timeout"

        [<Test>]
        let ``retrieve-real-data`` () = 
            let eikon = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI 
            let q = OuterLoader(eikon) :> MetaLoader
            try
                Async.RunSynchronously (Dex2Tests.test q, 30000) |> should be True
            with :? TimeoutException -> 
                logger.Error "...timeout"
                Assert.Fail "Timeout"
