namespace YieldMap.Tests.Unit

open System
open System.IO
open System.Xml

open YieldMap.Tests.Common

open NUnit.Framework
open FsUnit

module MetaChainsTests = 
    open YieldMap.Loader.SdkFactory
    open YieldMap.Requests.Requests
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.Calendar

    open YieldMap.Requests.MetaTables
        
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "Dex2Tests"

    [<Test>]
    let ``Requested chain is recieved`` () = 
        try
            let dt = DateTime(2014,3,4)
            let f = MockFactory() :> EikonFactory
            let l = MockChainMeta(dt) :> ChainMetaLoader

            globalThreshold := LoggingLevel.Debug

            Dex2Tests.test f l "0#RUTSY=MM" |> Async.RunSynchronously |> should be True
        with e -> 
            logger.ErrorF "Failed %s" (e.ToString())
            Assert.Fail()
        
    [<TestCase(0, 0, 126)>]
    [<TestCase(1, 0, 87)>]
    [<TestCase(0, 1, 39)>]
    [<TestCase(1, 1, 0)>]
    let ``Chains come up in parallel and if some fails, the other don't`` (t1 : int, t2 : int, cnt : int) =
        logger.TraceF "Testing chain timeout %A -> %A -> %d" t1 t2 cnt

        let dt = DateTime(2014,3,4)
        let f = MockFactory() :> EikonFactory
        let l = MockChainMeta(dt) :> ChainMetaLoader
        let chain name timeout = Dex2Tests.getChain l { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = timeout }
                
        let tasks = [ chain "0#RUTSY=MM" t1; chain "0#RUSOVB=MM" t2 ]
        let data = tasks |> Async.Parallel |> Async.RunSynchronously |> Array.collect id
        printfn "%d : %A" (Array.length data) data
        data |> Array.length |> should equal cnt
