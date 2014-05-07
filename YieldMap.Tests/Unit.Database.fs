namespace YieldMap.Tests.Unit

open System
open NUnit.Framework
open FsUnit

module DbTests =
    open YieldMap.Requests.MetaTables

    open YieldMap.Loader.Requests
    open YieldMap.Loader.MetaChains
    open YieldMap.Loader.SdkFactory
        
    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Location
        
    open YieldMap.Database


    let logger = LogFactory.create "TestDb"

    [<Test>]
    let ``Reading and writing to Db works`` () = 
        MainEntities.SetVariable("PathToTheDatabase", Location.path)
        let cnnStr = MainEntities.GetConnectionString("TheMainEntities")
        use ctx = new MainEntities(cnnStr)

        let cnt boo = query { for x in boo do select x; count }

        let count = cnt ctx.Chains
        logger.InfoF "Da count is %d" count

        let c = Feed (Name = Guid.NewGuid().ToString())
        let c = ctx.Feeds.Add c
        logger.InfoF "First c is <%d; %s>" c.id c.Name
        ctx.SaveChanges() |> ignore

        logger.InfoF "Now c is <%d; %s>" c.id c.Name
        let poo =  cnt ctx.Feeds
        logger.InfoF "Da count is now %d" poo
        poo |> should equal (count+1)

        let c = ctx.Feeds.Remove (c)
        ctx.SaveChanges() |> ignore
        logger.InfoF "And now c is <%d; %s>" c.id c.Name
        let poo =  cnt ctx.Chains
        logger.InfoF "Da count is now %d" poo
        poo |> should equal count

    [<Test>]
    let ``Load and save data to raw db`` ()   = 
        MainEntities.SetVariable("PathToTheDatabase", Location.path)
        let cnnStr = MainEntities.GetConnectionString("TheMainEntities")
          
        let connect (q:EikonFactory) = async {
            logger.TraceF "Connection request sent"
            let! connectRes = q.Connect()
            match connectRes with
            | Connection.Connected -> return true
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

        let saveBondDescrs (descrs : BondDescr list) = 
            use ctx = new MainEntities(cnnStr)

            descrs |> List.iter (fun item -> 
                // todo in case it takes much time, this might be somehow optimized
                // todo I can for example use Database "raw" classes as requests...
                // but then they will have to reference Loader.
                // hmmmmmm.... well, it is possible
                // but maybe i will just delete those "Raw" tables ))
                try
                    let rawBond =
                        new RawBondInfo ( 
                            BondStructure = item.BondStructure,
                            RateStructure = item.RateStructure,
                            IssueSize = item.IssueSize,
                            IssuerName = item.IssuerName,
                            BorrowerName = item.BorrowerName,
                            Coupon = item.Coupon,
                            Issue = item.Issue,
                            Maturity = item.Maturity,
                            Currency = item.Currency,
                            ShortName = item.ShortName,
                            IsCallable = item.IsCallable,
                            IsPutable = item.IsPutable,
                            IsFloater = item.IsFloater,
                            IsConvertible = item.IsConvertible,
                            IsStraight = item.IsStraight,
                            Ticker = item.Ticker,
                            Series = item.Series,
                            BorrowerCountry = item.BorrowerCountry,
                            IssuerCountry = item.IssuerCountry,
                            Isin = item.Isin,
                            ParentTicker = item.ParentTicker,
                            Seniority = item.Seniority,
                            Industry = item.Industry,
                            SubIndustry = item.SubIndustry,
                            Instrument = item.Instrument,
                            Ric = item.Ric                                
                        )
                    ctx.RawBondInfoes.Add rawBond |> ignore
                with e -> logger.ErrorEx "Failed to add" e
            )
            ctx.SaveChanges () 

        let saveIssueRatings (ratings : IssueRatingData list) = 
            use ctx = new MainEntities(cnnStr)
            ratings |> List.iter (fun item -> 
                let bondId = query {
                    for bond in ctx.RawBondInfoes do
                    where (bond.Ric = item.Ric)
                    select bond.id
                    exactlyOneOrDefault
                }
                if bondId > 0L then
                    let rawIssueRating = 
                        RawRating (
                            Date = item.RatingDate,
                            Rating = item.Rating,
                            Source = item.RatingSourceCode,
                            Issue = Nullable true,
                            id_RawBond = Nullable bondId
                        )
                    ctx.RawRatings.Add rawIssueRating |> ignore
            )
            ctx.SaveChanges () 

        // I am mighty! I have a glow you cannot see. I have a heart as big as the
        // moon, as warm as bathwater.

        let saveIssuerRatings (ratings : IssuerRatingData list) = 
            use ctx = new MainEntities(cnnStr)
            ratings |> List.iter (fun item -> 
                let bondId = query {
                    for bond in ctx.RawBondInfoes do
                    where (bond.Ric = item.Ric)
                    select bond.id
                    exactlyOneOrDefault
                }
                if bondId > 0L then
                    let rawIssueRating = 
                        RawRating (
                            Date = item.RatingDate,
                            Rating = item.Rating,
                            Source = item.RatingSourceCode,
                            Issue = Nullable false,
                            id_RawBond = Nullable bondId
                        )
                    ctx.RawRatings.Add rawIssueRating |> ignore
            )
            ctx.SaveChanges () 

        let saveFrns (frns : FrnData list) = 
            use ctx = new MainEntities(cnnStr)
            frns |> List.iter (fun item -> 
                let bondId = query {
                    for bond in ctx.RawBondInfoes do
                    where (bond.Ric = item.Ric)
                    select bond.id
                    exactlyOneOrDefault
                }   
                if bondId > 0L then
                    let rawFrn = 
                        RawFrnData (
                            Cap = item.Cap,
                            Floor = item.Floor,
                            Frequency = item.Frequency,
                            Margin = item.Margin,
                            Index = item.IndexRic,
                            id_RawBond = Nullable bondId
                        )
                    ctx.RawFrnDatas.Add rawFrn |> ignore
            )
            ctx.SaveChanges () 

//            let saveRicData (rics : RicData list) = imperative {
//                use ctx = new MainEntities(cnnStr)
//                rics |> List.iter (fun item -> 
//                    let bondId = query {
//                        for bond in ctx.RawBonds do
//                        where (bond.Ric = item.Ric)
//                        select bond.id
//                        exactlyOneOrDefault
//                    }   
//                    if bondId > 0L then
//                        let rawRics = 
//                            Raw (
//                                Cap = item.Cap,
//                                Floor = item.Floor,
//                                Frequency = item.Frequency,
//                                Margin = item.Margin,
//                                Index = item.IndexRic,
//                                id_RawBond = Nullable bondId
//                            )
//                        ctx.RawRicData.Add rawFrn |> ignore
//                )
//                return ctx.SaveChanges () 
//            }

        let dt = DateTime(2014,3,4)
        let f = MockFactory() :> EikonFactory
        let l = MockChainMeta(dt) :> ChainMetaLoader

        logger.TraceF "Before imperative"

        let condition c = if not c then failwith "Condition failed" 

        let request chainName = async {
            logger.TraceF "Before connection"
            let! connected =  connect f
            condition (connected)
            logger.TraceF "After connection"

            let! rics = getChain l { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = chainName; Timeout = 0 }
            condition (Array.length rics <> 0)
            logger.TraceF "After chain %s" chainName
                
            logger.InfoF "Loading BondDescr table"
            let! meta = l.LoadMetadata<BondDescr> rics
            condition (Meta.isAnswer meta)
            let descrs = Meta<BondDescr>.getAnswer meta
            let res = saveBondDescrs descrs
            res |> should equal (List.length descrs) // ezerisink eddid

//                logger.InfoF "Loading CouponData table"
//                let! meta = Parallel (l.LoadMetadata<CouponData> rics None)
//                condition (Meta.isAnswer meta)
//                let coupons = Meta<BondDescr>.getAnswer meta
//                logger.TraceF "CouponData is %A" coupons
//                let! res = saveCoupons coupons
//                
            logger.InfoF "Loading IssueRatingData table"
            let! meta = l.LoadMetadata<IssueRatingData> rics
            condition (Meta.isAnswer meta)
            let ratings = Meta<IssueRatingData>.getAnswer meta
            logger.TraceF "IssueRatingData is %A" ratings
            let res = saveIssueRatings ratings
            res |> should equal (List.length ratings) // ezerisink eddid
                       
            logger.InfoF "Loading IssuerRatingData table"
            let! meta = l.LoadMetadata<IssuerRatingData> rics 
            condition (Meta.isAnswer meta)
            let ratings = Meta<IssuerRatingData>.getAnswer meta
            logger.TraceF "IssuerRatingData is %A" ratings
            let res = saveIssuerRatings ratings
            res |> should equal (List.length ratings) // ezerisink eddid
                
            logger.InfoF "Loading FrnData table"
            let! meta = l.LoadMetadata<FrnData> rics
            condition (Meta.isAnswer meta)
            let frns = Meta<FrnData>.getAnswer meta
            logger.TraceF "FrnData is %A" frns
            let res = saveFrns frns
            res |> should equal (List.length frns) // ezerisink eddid

            // TODO LOAD ALL RICS FOR ONLY THOSE RICS WHICH ARE IN OPENED PORTFOLIO
                
//                logger.InfoF "Loading RicData table"
//                let! meta = Parallel (l.LoadMetadata<RicData> rics None)
//                condition (Meta.isAnswer meta)
//                let ricData = Meta<RicData>.getAnswer meta
//                logger.TraceF "RicData is %A" ricData
//                let! res = saveRicData ricData
        }

        let res = request "0#RUAER=MM" |> Async.Catch |> Async.RunSynchronously
        match res with
        | Choice2Of2 e -> 
            logger.ErrorEx "Failed" e
            Assert.Fail ()
        | _ -> ()