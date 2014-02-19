namespace YieldMap.Tests.Common

open System
open System.IO
open System.Xml

open YieldMap.Data.Answers
open YieldMap.Data.Requests
open YieldMap.Data.Loading
open YieldMap.Data.MetaTables
open YieldMap.Data

module Dex2Tests = 
    let connect (q:MetaLoader) = async {
        printfn "Connection request sent"
        let! connectRes = q.Connect()
        match connectRes with
        | Connection.Connected -> 
            printfn "Connected"
            return true
        | Connection.Failed e -> 
            printfn "Failed to connect %s" <| e.ToString()        
            return false
    }

    let getChain (q:MetaLoader) request = async {
        let! chain = q.LoadChain request
        match chain with
        | Chain.Answer data -> 
            return data
        | Chain.Failed e -> 
            printfn "Failed to load chain: %s" e.Message
            return [||]
    }

    let test (q:MetaLoader) = async {
        printfn "Connection request sent"
        let! connected = connect q
        if not connected then return ()

        let! data = getChain q { Feed = "IDN"; Mode = ""; Ric = "0#RUCORP=MM" }
        if Array.length data = 0 then return ()

        printfn "Chain %A" data
        let! meta = q.LoadMetadata<BondDescr> data
        match meta with
        | Meta.Answer metaData -> printfn "BondDescr is %A" metaData
        | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()

        let! meta = q.LoadMetadata<CouponData> data
        match meta with
        | Meta.Answer metaData -> printfn "CouponData is %A" metaData
        | Meta.Failed e -> printfn "Failed to load meta: %s" <| e.ToString()
    } 