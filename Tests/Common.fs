namespace YieldMap.Tests.Common

open System
open System.IO
open System.Xml

open YieldMap.Data.Answers
open YieldMap.Data.Requests
open YieldMap.Data.Loading
open YieldMap.Data.MetaTables
open YieldMap.Data
open YieldMap.Tools.Logging

module Dex2Tests = 
    let logger = LogFactory.create "Dex2Tests" //(Sinker.nLogSink "Dex2Tests" "unit-testing.log")

    let connect (q:MetaLoader) = async {
        logger.Trace "Connection request sent"
        let! connectRes = q.Connect()
        match connectRes with
        | Connection.Connected -> 
            logger.Trace "Connected"
            return true
        | Connection.Failed e -> 
            logger.Trace <| sprintf "Failed to connect %s" (e.ToString())
            return false
    }

    let getChain (q:MetaLoader) request = async {
        let! chain = q.LoadChain request
        match chain with
        | Chain.Answer data -> 
            return data
        | Chain.Failed e -> 
            logger.Trace <| sprintf "Failed to load chain: %s" e.Message
            return [||]
    }

    let test (q:MetaLoader) chainName = async {
        let! connected = connect q
        logger.Trace "After connection"
        if connected then
            logger.Trace <| sprintf "Before chain %s" chainName
            // todo strange when feed is Q it just hangs, it doesn't report any error...
            // todo maybe I should always chech if the feed is alive via AdxRtSourceList???
            let! data = getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = chainName }
            logger.Trace "After chain"
            if Array.length data <> 0 then
                let success = ref true
                logger.Trace <| sprintf "Chain %A" data

//                logger.Info "Loading BondDescr table"
//                let! meta = q.LoadMetadata<BondDescr> data
//                match meta with
//                | Meta.Answer metaData -> logger.Trace <| sprintf "BondDescr is %A" metaData
//                | Meta.Failed e -> 
//                    logger.Error <| sprintf "Failed to load BondDescr: %s" (e.ToString())
//                    success := false
//
//                logger.Info "Loading CouponData table"
//                let! meta = q.LoadMetadata<CouponData> data
//                match meta with
//                | Meta.Answer metaData -> logger.Trace <| sprintf "CouponData is %A" metaData
//                | Meta.Failed e -> 
//                    logger.Error <| sprintf "Failed to load CouponData: %s" (e.ToString())
//                    success := false
//        
//                logger.Info "Loading IssueRatingData table"
//                let! meta = q.LoadMetadata<IssueRatingData> data
//                match meta with
//                | Meta.Answer metaData -> logger.Trace <| sprintf "IssueRatingData is %A" metaData
//                | Meta.Failed e -> 
//                    logger.Error <| sprintf "Failed to load IssueRatingData: %s" (e.ToString())
//                    success := false
//        
//                logger.Info "Loading IssuerRatingData table"
//                let! meta = q.LoadMetadata<IssuerRatingData> data
//                match meta with
//                | Meta.Answer metaData -> logger.Trace <| sprintf "IssuerRatingData is %A" metaData
//                | Meta.Failed e -> 
//                    logger.Error <| sprintf "Failed to load IssuerRatingData: %s" (e.ToString())
//                    success := false
//
//                logger.Info "Loading FrnData table"
//                let! meta = q.LoadMetadata<FrnData> data
//                match meta with
//                | Meta.Answer metaData -> logger.Trace <| sprintf "FrnData is %A" metaData
//                | Meta.Failed e -> 
//                    logger.Error <| sprintf "Failed to load FrnData: %s" (e.ToString())
//                    success := false
        
                logger.Info "Loading RicData table"
                let! meta = q.LoadMetadata<RicData> data
                match meta with
                | Meta.Answer metaData -> logger.Trace <| sprintf "RicData is %A" metaData
                | Meta.Failed e -> 
                    logger.Error <| sprintf "Failed to load RicData: %s" (e.ToString())
                    success := false
        
                return !success
            else return false
        else 
            logger.Trace "Not connected"
            return false
    } 