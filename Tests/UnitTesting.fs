namespace YieldMap.Tests.Common

    module DataTests = 
        open System
        open System.IO
        open System.Xml

        open YieldMap.Data.Answers
        open YieldMap.Data.Requests
        open YieldMap.Data.Loading
        open YieldMap.Data.MetaTables
        open YieldMap.Data

        open NUnit.Framework
        open FsUnit

        [<Test>]
        let ``connection`` () = 
            let q = MockLoader() :> MetaLoader
            let ans =  Dex2Tests.connect q |> Async.RunSynchronously
            ans |> should be True

        [<Test>]
        let ``retrieve-mock-data`` () = 
            let q = MockLoader() :> MetaLoader
            async {
                do! Dex2Tests.test q
            } |> Async.RunSynchronously
            true |> should be True
