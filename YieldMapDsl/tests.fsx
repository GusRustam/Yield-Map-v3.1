#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map\Tools\YieldMapDsl\YieldMapDsl\bin\Debug\YieldMapDsl.dll"
#endif

open System
open System.IO
open System.Xml

open EikonDesktopDataAPI

open YieldMap.Data.Answers
open YieldMap.Data.Requests
open YieldMap.Data.Loading
open YieldMap.Data.MetaTables
open YieldMap.Data

let test (q:MetaLoader) = async {
    printfn "Connection request sent"
    let! connectRes = q.Connect()
    match connectRes with
    | Connection.Connected -> 
        let! chain = q.LoadChain { Feed = "IDN"; Mode = ""; Ric = "0#RUCORP=MM" }
        match chain with
        | Chain.Answer data -> 
            printfn "Chain %A" data
            let! meta = q.LoadMetadata<BondDescr> data
            match meta with
            | Meta.Answer metaData -> printfn "BondDescr is %A" metaData
            | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()

            let! meta = q.LoadMetadata<CouponData> data
            match meta with
            | Meta.Answer metaData -> printfn "CouponData is %A" metaData
            | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()
        | Chain.Failed e -> printfn "Failed to load chain: %s" e.Message
    | Connection.Failed e -> printfn "Failed to connect %s" <| e.ToString()
} 



let q = MockLoader() :> MetaLoader
//let e = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI
//let q = OuterLoader(e) :> MetaLoader

test q |> Async.Start