namespace YieldMap.Tests.Common

open System
open System.IO
open System.Xml

open EikonDesktopDataAPI

open NUnit.Framework

open YieldMap.Loader.Requests
open YieldMap.Loader.SdkFactory
open YieldMap.Loader.MetaChains
open YieldMap.Loader.MetaTables
open YieldMap.Tools.Aux
open YieldMap.Tools.Logging

module Dex2Tests = 
    let logger = LogFactory.create "Dex2Tests"

    let connect (q:EikonFactory) = async {
        logger.TraceF "Connection request sent"
        let! connectRes = q.Connect()
        match connectRes with
        | Connection.Connected -> 
            logger.TraceF "Connected"
            return true
        | Connection.Failed e -> 
            logger.TraceF "Failed to connect %s" (e.ToString())
            return false
    }

    let getChain (q:ChainMetaLoader) request = async {
        let! chain = q.LoadChain request
        match chain with
        | Chain.Answer data -> return data
        | Chain.Failed e -> 
            logger.TraceF "Failed to load chain: %s" e.Message
            return [||]
    }

    let test (f:EikonFactory) (q:ChainMetaLoader) chainName = async {
        let! connected = connect f
        logger.TraceF "After connection"
        if connected then
            logger.TraceF "Before chain %s" chainName
            // todo strange when feed is Q it just hangs, it doesn't report any error...
            // todo maybe I should always chech if the feed is alive via AdxRtSourceList???
            let! data = getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = chainName; Timeout = 0 }
            logger.TraceF "After chain"
            if Array.length data <> 0 then
                let success = ref true
                logger.TraceF "Chain %A" data

                logger.InfoF "Loading BondDescr table"
                let! meta = q.LoadMetadata<BondDescr> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "BondDescr is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load BondDescr: %s" (e.ToString())
                    success := false

                logger.InfoF "Loading CouponData table"
                let! meta = q.LoadMetadata<CouponData> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "CouponData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load CouponData: %s" (e.ToString())
                    success := false
        
                logger.InfoF "Loading IssueRatingData table"
                let! meta = q.LoadMetadata<IssueRatingData> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "IssueRatingData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load IssueRatingData: %s" (e.ToString())
                    success := false
        
                logger.InfoF "Loading IssuerRatingData table"
                let! meta = q.LoadMetadata<IssuerRatingData> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "IssuerRatingData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load IssuerRatingData: %s" (e.ToString())
                    success := false

                logger.InfoF "Loading FrnData table"
                let! meta = q.LoadMetadata<FrnData> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "FrnData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load FrnData: %s" (e.ToString())
                    success := false
        
                logger.InfoF "Loading RicData table"
                let! meta = q.LoadMetadata<RicData> data 
                match meta with
                | Meta.Answer metaData -> logger.TraceF "RicData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load RicData: %s" (e.ToString())
                    success := false
        
                return !success
            else return false
        else 
            logger.TraceF "Not connected"
            return false
    } 