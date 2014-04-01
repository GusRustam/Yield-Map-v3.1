namespace YieldMap.Tests.Common

open System
open System.IO
open System.Xml

open EikonDesktopDataAPI

open NUnit.Framework

open YieldMap.Requests
open YieldMap.Requests.Answers
open YieldMap.Loading
open YieldMap.MetaTables
open YieldMap.Loading.SdkFactory
open YieldMap.Tools
open YieldMap.Logging

module Dex2Tests = 
    let logger = LogFactory.create "Dex2Tests"

    let connect (q:SdkFactory.Loader) = async {
        logger.Trace "Connection request sent"
        let! connectRes = q.Connect()
        match connectRes with
        | Connection.Connected -> 
            logger.Trace "Connected"
            return true
        | Connection.Failed e -> 
            logger.TraceF "Failed to connect %s" (e.ToString())
            return false
    }

//    let executeTestInMock dt test = 
//        let loader = MockOnlyFactory(dt) :> Loader
//        if connect loader |> Async.RunSynchronously then
//            test loader
//        else Assert.Fail()
//
//    let executeTestInEikon test = 
//        let eikon = ref (EikonDesktopDataAPIClass() :> EikonDesktopDataAPI)
//        try
//            let loader = OuterEikonFactory(!eikon) :> Loader
//            if connect loader |> Async.RunSynchronously then
//                test loader
//            else Assert.Fail()
//        finally
//            Ole32.killComObject eikon
//            Ole32.CoUninitialize()
//

    let getChain (q:SdkFactory.Loader) request = async {
        let! chain = q.LoadChain request
        match chain with
        | Chain.Answer data -> 
            return data
        | Chain.Failed e -> 
            logger.TraceF "Failed to load chain: %s" e.Message
            return [||]
    }

    let test (q:SdkFactory.Loader) chainName = async {
        let! connected = connect q
        logger.Trace "After connection"
        if connected then
            logger.TraceF "Before chain %s" chainName
            // todo strange when feed is Q it just hangs, it doesn't report any error...
            // todo maybe I should always chech if the feed is alive via AdxRtSourceList???
            let! data = getChain q { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = chainName; Timeout = None }
            logger.Trace "After chain"
            if Array.length data <> 0 then
                let success = ref true
                logger.TraceF "Chain %A" data

                logger.Info "Loading BondDescr table"
                let! meta = q.LoadMetadata<BondDescr> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "BondDescr is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load BondDescr: %s" (e.ToString())
                    success := false

                logger.Info "Loading CouponData table"
                let! meta = q.LoadMetadata<CouponData> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "CouponData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load CouponData: %s" (e.ToString())
                    success := false
        
                logger.Info "Loading IssueRatingData table"
                let! meta = q.LoadMetadata<IssueRatingData> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "IssueRatingData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load IssueRatingData: %s" (e.ToString())
                    success := false
        
                logger.Info "Loading IssuerRatingData table"
                let! meta = q.LoadMetadata<IssuerRatingData> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "IssuerRatingData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load IssuerRatingData: %s" (e.ToString())
                    success := false

                logger.Info "Loading FrnData table"
                let! meta = q.LoadMetadata<FrnData> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "FrnData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load FrnData: %s" (e.ToString())
                    success := false
        
                logger.Info "Loading RicData table"
                let! meta = q.LoadMetadata<RicData> data None
                match meta with
                | Meta.Answer metaData -> logger.TraceF "RicData is %A" metaData
                | Meta.Failed e -> 
                    logger.ErrorF "Failed to load RicData: %s" (e.ToString())
                    success := false
        
                return !success
            else return false
        else 
            logger.Trace "Not connected"
            return false
    } 