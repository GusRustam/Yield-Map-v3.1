namespace YieldMap.Tests.Common

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
        let ``retrieving-mock-data`` = 
            let e = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI
            let q = OuterLoader(e) :> MetaLoader // eikon
        //
        //    let tsk = Dex2Tests.connect q |> Async.StartAsTask
        //    if tsk.Wait(TimeSpan.FromSeconds(float 10)) then
        //        let res = tsk.Result
        //        if res then
        //            printfn "... connected"
        //        else
        //            printfn "... not connected"
        //    else
        //        printfn " ... timeout"

            try
                let res = Async.RunSynchronously(Dex2Tests.connect q, 10000)
                if res then
                    printfn "... connected"
                else
                    printfn "... not connected"
            with :? TimeoutException -> printfn "...timeout"