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

let q = MockLoader() :> MetaLoader
printfn "Connection request sent"
q.Connect() |> Async.RunSynchronously
printfn "Connected"
let chain = q.LoadChain { Feed = "IDN"; Mode = ""; Ric = "0#RUCORP=MM" } |> Async.RunSynchronously
printfn "Chain %A" chain