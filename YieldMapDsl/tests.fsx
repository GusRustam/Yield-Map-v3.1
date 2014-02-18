#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map\Tools\YieldMapDsl\YieldMapDsl\bin\Debug\YieldMapDsl.dll"
#endif

open System
open System.IO
open System.Xml

open YieldMap.Data.Answers
open YieldMap.Data.Requests
open YieldMap.Data.Loading
open YieldMap.Data.MetaTables

let q = MockLoader() :> MetaLoader
printfn "Connection request sent"
q.Connect() |> Async.RunSynchronously
printfn "Connected"
let chain = q.LoadChain { Feed = "IDN"; Mode = ""; Ric = "0#RUCORP=MM" } |> Async.RunSynchronously
match chain with
| Answer data -> 
    printfn "Chain %A" data
    let meta = q.LoadMetadata<BondDescr> data |> Async.RunSynchronously
    printfn "Meta is %A" meta
| Failed e -> printfn "Failed: %s" e.Message
