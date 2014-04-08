namespace YieldMap.Tests.Unit

    open System
    open System.IO
    open System.Xml

    open YieldMap.Tests.Common

    open NUnit.Framework
    open FsUnit

    module MetaChainsTests = 
        open YieldMap.Loader.Requests
        open YieldMap.Loader.SdkFactory
        open YieldMap.Loader.Requests
        open YieldMap.Loader.MetaChains
        open YieldMap.Loader.MetaTables
        open YieldMap.Loader.Calendar
        
        open YieldMap.Tools.Logging

        let logger = LogFactory.create "Dex2Tests"

        [<Test>]
        let ``Requested chain is recieved`` () = 
            try
                let dt = DateTime(2014,3,4)
                let f = MockFactory() :> EikonFactory
                let l = MockChainMeta() :> ChainMetaLoader

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
            let toSome t = if t <= 0 then None else Some t

            logger.TraceF "Testing chain timeout %A -> %A -> %d" (toSome t1) (toSome t2) cnt

            let dt = DateTime(2014,3,4)
            let f = MockFactory() :> EikonFactory
            let l = MockChainMeta() :> ChainMetaLoader
            let chain name timeout = Dex2Tests.getChain l { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = name; Timeout = timeout }
                
            let tasks = [ chain "0#RUTSY=MM" (toSome t1); chain "0#RUSOVB=MM" (toSome t2) ]
            let data = tasks |> Async.Parallel |> Async.RunSynchronously |> Array.collect id
            printfn "%d : %A" (Array.length data) data
            data |> Array.length |> should equal cnt

        [<Test>]
        let ``Tomorrow event happens in mock calendar`` () =
            let always x = fun _ -> x
            let count = ref 0

            let clndr = Calendar.MockCalendar(DateTime(2010, 1, 31, 23, 59, 55)) :> Calendar
            
            clndr.NewDay |> Observable.map (always 1) |> Observable.scan (+) 0 |> Observable.add (fun x -> count := x)
            clndr.NewDay |> Observable.add (fun dt -> logger.InfoF "Ping!!! %A" dt)

            Async.AwaitEvent clndr.NewDay |> Async.Ignore |> Async.Start
            Async.Sleep(10000) |> Async.RunSynchronously
            !count |> should equal 1

    module MockSubscriptionTest = 
        open YieldMap.Tools.Logging
        open YieldMap.Loader.LiveQuotes

        let logger = LogFactory.create "LiveQuotesTest"

        [<Test>]
        let ``I receive quotes I'm subscribed`` () =
            logger.InfoF "I receive quotes I subscribed"
            let slots = seq {
                yield { Interval = 1.0; Items = [{Ric="YYY";Field="BID";Value="11"}] }
                yield { Interval = 2.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
            }
            let generator = SeqGenerator (slots, false)
            use data = new MockSubscription(generator)
            let subscription = data :> Subscription
            let count = ref 0
            subscription.Add ([("XXX",["BID"])] |> Map.ofList)
            subscription.OnQuotes |> Observable.add (fun q -> 
                logger.InfoF "Got quote %A" q
                count := !count + 1)
            subscription.Start ()
            Async.Sleep 7000 |> Async.RunSynchronously
            !count |> should equal 2

        [<Test>]
        let ``I don't receive quotes I'm not subscribed`` () =
            logger.InfoF "I don't receive quotes I'm not subscribed"
            let slots = seq {
                yield { Interval = 1.0; Items = [{Ric="XXX";Field="ASK";Value="11"}] }
                yield { Interval = 2.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
            }
            let generator = SeqGenerator (slots, false)
            use data = new MockSubscription(generator)
            let subscription = data :> Subscription
            let count = ref 0
            subscription.Add ([("XXX",["BID"])] |> Map.ofList)
            subscription.OnQuotes |> Observable.add (fun q -> 
                logger.InfoF "Got quote %A" q
                count := !count + 1)
            subscription.Start ()
            Async.Sleep 7000 |> Async.RunSynchronously
            !count |> should equal 2

        [<Test>]
        let ``I stop receiveing quotes I've unsubscribed`` () =
            logger.InfoF "I stop receive quotes I've unsubscribed"
            let slots = seq {
                yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
                yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
            }
            let generator = SeqGenerator (slots, false)
            use data = new MockSubscription(generator)
            let subscription = data :> Subscription
            let count = ref 0
            subscription.Add ([("XXX",["BID"])] |> Map.ofList)
            subscription.OnQuotes |> Observable.add (fun q -> 
                logger.InfoF "Got quote %A" q
                count := !count + 1)

            subscription.Start ()
            Async.Sleep 6000 |> Async.RunSynchronously
            subscription.Remove ["XXX"]
            Async.Sleep 6000 |> Async.RunSynchronously

            !count |> should equal 1

        [<Test>]
        let ``I begin receiveing quotes I've subscribed to`` () =
            logger.InfoF "I begin receiveing quotes I've subscribed to"
            let slots = seq {
                yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
                yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
            }
            let generator = SeqGenerator (slots, false)
            use data = new MockSubscription(generator)
            let subscription = data :> Subscription
            let count = ref 0
            subscription.Add ([("XXX",["BID"])] |> Map.ofList)
            subscription.OnQuotes |> Observable.add (fun q -> 
                logger.InfoF "Got quote %A" q
                count := !count + 1)

            subscription.Start ()
            Async.Sleep 6000 |> Async.RunSynchronously
            subscription.Add ([("YYY",["ASK"])] |> Map.ofList)
            Async.Sleep 5000 |> Async.RunSynchronously

            !count |> should equal 3

        [<Test>]
        let ``I receive snapshots according to recent quotes`` () =
            logger.InfoF "I receive snapshots according to recent quotes"
            let slots = seq {
                yield { Interval = 2.0; Items = [{Ric="YYY";Field="ASK";Value="11"}] }
                yield { Interval = 3.0; Items = [{Ric="XXX";Field="BID";Value="12"}] }
                yield { Interval = 3.0; Items = [{Ric="YYY";Field="ASK";Value="22"}] }
            }

            let generator = SeqGenerator (slots, false)
            use data = new MockSubscription(generator)
            let subscription = data :> Subscription

            subscription.Start ()

            let x = subscription.Snapshot (([("XXX",["BID"])] |> Map.ofList), None) |> Async.RunSynchronously
            match x with Succeed rfv -> counts rfv |> should equal (0,0) | _ -> Assert.Fail()

            let x = subscription.Snapshot (([("XXX",["BID"]); ("ZZZ",["QQQ"])] |> Map.ofList), None) |> Async.RunSynchronously
            match x with Succeed rfv -> counts rfv |> should equal (0,0) | _ -> Assert.Fail()

            Async.Sleep 5200 |> Async.RunSynchronously

            let x = subscription.Snapshot (([("XXX",["BID"])] |> Map.ofList), None) |> Async.RunSynchronously
            match x with Succeed rfv -> counts rfv |> should equal (1,1) | _ -> Assert.Fail()

            Async.Sleep 3500 |> Async.RunSynchronously

            let x = subscription.Snapshot (([("XXX",["BID"]); ("YYY",["ASK"])] |> Map.ofList), None) |> Async.RunSynchronously
            logger.InfoF "Got snapshot %A" x
            match x with 
            | Succeed rfv -> 
                counts rfv |> should equal (2,2) 
                rfv.["YYY"].["ASK"] |> should equal "22"
            | _ -> Assert.Fail()

            let x = subscription.Snapshot (([("YYY",["ASK"])] |> Map.ofList), None) |> Async.RunSynchronously
            match x with 
            | Succeed rfv -> counts rfv |> should equal (1,1) 
            | _ -> Assert.Fail()

    module TestWebServer = 
        open System.Net
        open System.Text

        open YieldMap.Tools.Logging
        open YieldMap.Loader.WebServer
        open YieldMap.Tools.Aux

        let logger = LogFactory.create "TestWebServer"

        [<Test>]
        let ``Web server starts, responds and stops`` () = 
            ApiServer.start ()
            logger.InfoF "Server started"
            Async.Sleep(3000) |> Async.RunSynchronously

            use wb = new WebClient()
            let res = wb.DownloadString(ApiServer.host)
            logger.InfoF "Got answer %A" res
            res.Substring(0,3) |> should equal "ERR"

            ApiServer.stop ()
            logger.InfoF "Server stopping"
            Async.Sleep(3000) |> Async.RunSynchronously
            logger.InfoF "Server must be stopped"

            let req = wb.AsyncDownloadString <| Uri ApiServer.host
            try
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                let q = Async.RunSynchronously(req, 5000)
                logger.ErrorF "got illegal answer %A" q
                Assert.Fail()
            with :? TimeoutException ->
                logger.InfoF "Done"

        [<Test>]
        let ``Web server accepts quotes`` () = 
            ApiServer.start ()
            logger.InfoF "Seems 2B started"
            Async.Sleep(5000) |> Async.RunSynchronously

            logger.InfoF "Sending request"
            use wb = new WebClient()

            let q = ApiQuote.create "XXX" "FLD" "12"
            let z = ApiQuotes.create [|q|]
            let enc = ApiQuotes.pack z

            let resp = wb.UploadData(ApiServer.host + "quote", enc)
            let response = Encoding.ASCII.GetString(resp)

            logger.InfoF "Response is %s" response
            response |> should equal "OK"

            ApiServer.stop ()

    module TestApiQuotes =
        open System.Net
        open System.Text
        open System.Threading

        open YieldMap.Tools.Logging
        open YieldMap.Loader.LiveQuotes
        open YieldMap.Tools.Aux
        open YieldMap.Loader.WebServer

        let logger = LogFactory.create "TestApiQuotes"

        [<Test>]
        let ``I recieve quotes I sent`` () = 
            let apiQuotes = ApiSubscription() :> Subscription

            let slots = [|
                ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "12"|]
                ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "13"|]
                ApiQuotes.create [|ApiQuote.create "XXX" "BID" "95"|]
                ApiQuotes.create [|
                    ApiQuote.create "YYY" "BID" "10"
                    ApiQuote.create "YYY" "ASK" "15"
                    ApiQuote.create "XXX" "ASK" "105"
                |]
            |]

            let count = ref 0

            let subscription = [
                ("YYY", ["BID"; "ASK"])
                ("XXX", ["BID"; "ASK"])
            ]

            subscription |> Map.ofList |> apiQuotes.Add 
            apiQuotes.Start()
            apiQuotes.OnQuotes 
            |> Observable.scan (fun (count, item) evt -> (count+1, evt)) (-1, Map.empty)
            |> Observable.add (fun (i, rfv) -> 
                count := i+1
                logger.InfoF "Got quotes %A" rfv
                try rfv |> should equal (ApiQuotes.toRfv slots.[i])
                with e -> logger.ErrorEx "Failed" e
                logger.InfoF "Ok"
            )

            use wb = new WebClient()

            slots |> Array.iter (fun slot -> 
                logger.InfoF "To send quotes %A" slot
                let enc = ApiQuotes.pack slot
                let resp = wb.UploadData(ApiServer.host + "quote", enc)
                let response = Encoding.ASCII.GetString(resp)
                response |> should equal "OK"
                Async.Sleep(1000) |> Async.RunSynchronously
            )

            !count |> should equal (Array.length slots)
            apiQuotes.Stop()

    module DbTests =
        open YieldMap.Loader.Requests
        open YieldMap.Loader.MetaTables
        open YieldMap.Loader.MetaChains
        open YieldMap.Loader.SdkFactory
        
        open YieldMap.Tools.Aux
        open YieldMap.Tools.Aux.Workflows.AsyncAttempt
        open YieldMap.Tools.Logging
        open YieldMap.Tools.Location
        
        open YieldMap.Database


        let logger = LogFactory.create "TestApiQuotes"

        [<Test>]
        let ``Reading and writing to Db works`` () = 
            MainEntities.SetVariable("PathToTheDatabase", Location.path)
            let cnnStr = MainEntities.GetConnectionString("TheMainEntities")
            use ctx = new MainEntities(cnnStr)

            let cnt boo = query { for x in boo do select x; count }

            let count = cnt ctx.RefChains
            logger.InfoF "Da count is %d" count

            let c = RefChain()
            c.Name <- Guid.NewGuid().ToString()
            let c = ctx.RefChains.Add c
            logger.InfoF "First c is <%d; %s>" c.id c.Name
            ctx.SaveChanges() |> ignore

            logger.InfoF "Now c is <%d; %s>" c.id c.Name
            let poo =  cnt ctx.RefChains
            logger.InfoF "Da count is now %d" poo
            poo |> should equal (count+1)

            let c = ctx.RefChains.Remove (c)
            ctx.SaveChanges() |> ignore
            logger.InfoF "And now c is <%d; %s>" c.id c.Name
            let poo =  cnt ctx.RefChains
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

            let saveBondDescrs (descrs : BondDescr list) = imperative {  
                use ctx = new MainEntities(cnnStr)

                descrs |> List.iter (fun item -> 
                    // todo in case it takes much time, this might be somehow optimized
                    // todo I can for example use Database "raw" classes as requests...
                    // but then they will have to reference Loader.
                    // hmmmmmm.... well, it is possible
                    // but maybe i will just delete those "Raw" tables ))
                    let rawBond =
                        new RawBond ( 
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
                    ctx.RawBonds.Add rawBond |> ignore
                )
                return ctx.SaveChanges () 
            }

            let saveIssueRatings (ratings : IssueRatingData list ) = imperative {
                use ctx = new MainEntities(cnnStr)
                ratings |> List.iter (fun item -> 
                    let bondId = query {
                        for bond in ctx.RawBonds do
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
                return ctx.SaveChanges () 
            }

            let saveIssuerRatings (ratings : IssuerRatingData list ) = imperative {
                use ctx = new MainEntities(cnnStr)
                ratings |> List.iter (fun item -> 
                    let bondId = query {
                        for bond in ctx.RawBonds do
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
                return ctx.SaveChanges () 
            }

            let saveFrns (frns : FrnData list) = imperative {
                use ctx = new MainEntities(cnnStr)
                frns |> List.iter (fun item -> 
                    let bondId = query {
                        for bond in ctx.RawBonds do
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
                        ctx.RawFrnData.Add rawFrn |> ignore
                )
                return ctx.SaveChanges () 
            }

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
            let request chainName = imperative {
                logger.TraceF "Before connection"
                let! connected = Parallel <| connect f
                condition (connected)
                logger.TraceF "After connection"

                let! rics = Parallel <| getChain l { Feed = "IDN"; Mode = "UWC:YES LAY:VER"; Ric = chainName; Timeout = None }
                condition (Array.length rics <> 0)
                logger.TraceF "After chain %s" chainName
                
                logger.InfoF "Loading BondDescr table"
                let! meta = Parallel (l.LoadMetadata<BondDescr> rics None)
                condition (Meta.isAnswer meta)
                let descrs = Meta<BondDescr>.getAnswer meta
                let! res = saveBondDescrs descrs
                res |> should equal (List.length descrs) // ezerisink eddid

//                logger.InfoF "Loading CouponData table"
//                let! meta = Parallel (l.LoadMetadata<CouponData> rics None)
//                condition (Meta.isAnswer meta)
//                let coupons = Meta<BondDescr>.getAnswer meta
//                logger.TraceF "CouponData is %A" coupons
//                let! res = saveCoupons coupons
//                
                logger.InfoF "Loading IssueRatingData table"
                let! meta = Parallel (l.LoadMetadata<IssueRatingData> rics None)
                condition (Meta.isAnswer meta)
                let ratings = Meta<IssueRatingData>.getAnswer meta
                logger.TraceF "IssueRatingData is %A" ratings
                let! res = saveIssueRatings ratings
                res |> should equal (List.length ratings) // ezerisink eddid
                       
                logger.InfoF "Loading IssuerRatingData table"
                let! meta = Parallel (l.LoadMetadata<IssuerRatingData> rics None)
                condition (Meta.isAnswer meta)
                let ratings = Meta<IssuerRatingData>.getAnswer meta
                logger.TraceF "IssuerRatingData is %A" ratings
                let! res = saveIssuerRatings ratings
                res |> should equal (List.length ratings) // ezerisink eddid
                
                logger.InfoF "Loading FrnData table"
                let! meta = Parallel (l.LoadMetadata<FrnData> rics None)
                condition (Meta.isAnswer meta)
                let frns = Meta<FrnData>.getAnswer meta
                logger.TraceF "FrnData is %A" frns
                let! res = saveFrns frns
                res |> should equal (List.length frns) // ezerisink eddid

                // TODO LOAD ALL RICS FOR ONLY THOSE RICS WHICH ARE IN OPENED PORTFOLIO
                
//                logger.InfoF "Loading RicData table"
//                let! meta = Parallel (l.LoadMetadata<RicData> rics None)
//                condition (Meta.isAnswer meta)
//                let ricData = Meta<RicData>.getAnswer meta
//                logger.TraceF "RicData is %A" ricData
//                let! res = saveRicData ricData
                    
                return true          
            }

            let res = AsyncAttempt.runAttempt (request "0#RUAER=MM") None
            res |> should equal (Some true)