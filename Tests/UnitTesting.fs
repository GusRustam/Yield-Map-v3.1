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

        [<Test>]
        let ``retrieve-mock-data`` () = 
            let dt = DateTime(2014,3,4)
            let q = MockLoader(Some dt) :> MetaLoader

            logger.Trace "This should be visible"
            Logging.globalThreshold := Logging.LoggingLevel.Off
            logger.Trace "This should be invisible"

            Dex2Tests.test q "0#RUCORP=MM" |> Async.RunSynchronously |> should be True