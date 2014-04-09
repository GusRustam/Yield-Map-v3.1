namespace YieldMap.Core.Axis
    open System

//    [<AutoOpen>]
//    module Axis = 
//        type PercentageUnit = 
//        | Percent 
//        | PercentPoint 
//        | BasisPoint
//        with 
//            static member convert a t f = 
//                match f with
//                | Percent -> 
//                    match t with
//                    | Percent -> a
//                    | PercentPoint -> a * 100M
//                    | BasisPoint -> a * 10000M
//                | PercentPoint -> 
//                    match t with
//                    | Percent -> a / 100M
//                    | PercentPoint -> a
//                    | BasisPoint -> a * 100M
//                | BasisPoint -> 
//                    match t with
//                    | Percent -> a / 10000M
//                    | PercentPoint -> a / 100M
//                    | BasisPoint -> a
//
//        let convertTime (a:TimeValue) (today:DateTime) (t : TimeValue) (f:TimeValue) = 
//            match f with
//            | DateValue(since) -> 
//                match t with
//                | DateValue(till) -> DateValue(till)
//                | IntervalValue(till, kind) -> 
//                    let tillAsDate = 
//                        match kind with
//                        | Date -> failwith ""
//                        | Day -> today.AddDays(float till)
//                        | Month -> today.AddMonths(till)
//                        | Year -> today.AddYears(till)
//                    tillAsDate - since
//                | Week -> a * 10000M
//                | Month -> a * 10000M
//                | Year -> a * 10000M

    module Ratings = 
        type RatingSource = 
            | SnP
            | Moody's
            | Fitch

            member this.Name = 
                match this with
                    | SnP -> "Standard and Poor's"
                    | Moody's -> "Moody's Investor Service"
                    | Fitch -> "Fitch Rating"

            member this.Abbr = 
                match this with
                    | SnP -> "SnP"
                    | Moody's -> "Moodys"
                    | Fitch -> "Fitch"

        type Notch = 
            | AAA 
            | Aa1  | Aa2  | Aa3
            | A1   | A2   | A3
            | Baa1 | Baa2 | Baa3
            | Ba1  | Ba2  | Ba3
            | B1   | B2   | B3
            | Caa1 | Caa2 | Caa3
            | Ca   | C
    
            static member values = [ 
                (AAA,  1000, ["AAA"]);
                (Aa1,  990,  ["AA+"; "Aa1"]);     (Aa2, 980, ["AA"; "Aa2"]);     (Aa3, 970, ["AA-"; "Aa3"]);
                (A1,   960,  ["A+"; "A1"]);        (A2, 950, ["A"; "A2"]);        (A3, 940, ["A-"; "A3"]);
                (Baa1, 930,  ["BBB+"; "Baa1"]);  (Baa2, 920, ["BBB"; "Baa2"]);  (Baa3, 910, ["BBB-"; "Baa3"]);
                (Ba1,  900,  ["BB+"; "Ba1"]);     (Ba2, 890, ["BB"; "Ba2"]);     (Ba3, 880, ["BB-"; "Ba3"]);
                (B1,   870,  ["B+"; "B1"]);        (B2, 860, ["B"; "B2"]);        (B3, 850, ["B-"; "B3"]);
                (Caa1, 840,  ["CCC+"; "Caa1"]);  (Caa2, 830, ["CCC"; "Caa2"]);  (Caa3, 820, ["CCC-"; "Caa3"]);
                (Ca,   810,  ["CC"; "Ca"]);
                (C,    800,  ["C"])
            ]    

            static member fromName (name : string) =
                let filter (_, _, names) = List.exists <| (=) name <| names

                match List.tryFind filter Notch.values with
                    | Some(n, _, _) -> Some(n)
                    | _ -> None
    
        type Rating = {
            Source : RatingSource; 
            Raing : string; 
            Date : DateTime
        }

    (* === Инструменты === *)
    module Instruments = 
        open Ratings
        open YieldMap.Core.Calculations

        type BondMetadata = {
            Isin : string
            Ric : string
            BondStructure : string
            RateStructure : string
            Maturity : DateTime option
            Issue : DateTime option
            Coupon : decimal 
            Currency : string 
            IssueRating : Rating option
            IssuerRating : Rating option
        } 

        type FrnMetadata = {
            Isin : string
            Ric : string
            IndexRic : string
            FrnStructure : string
            RateStructure : string
            Maturity : DateTime option
            Issue : DateTime option
            Currency : string 
            IssueRating : Rating option
            IssuerRating : Rating option
        } 

        type LegType = Fixed | Floating of string // index ric
        type IrsLeg = { LegType : LegType; Structure : string }
        type CcsLeg = { Currency:  string; LegType : LegType; Structure : string }
        
        type IrsMetadata = { 
            Ric : string
            PaidLeg : IrsLeg 
            ReceivedLeg : IrsLeg 
            BothLegs : string
            Currency : string
        }

        type CcsMetadata = { 
            Ric : string
            PaidLeg : CcsLeg
            ReceivedLeg : CcsLeg
            FxRate : decimal 
        }

        type FraMetadata = { 
            Ric : string
            IndexRic : string
            Structure : string 
        }

    (* === Вычисления === *)
    module Analytics = 
        open Instruments

//        type QuoteAdapter = abstract member Convert : decimal -> decimal
//        type IdentityAdapter() = interface QuoteAdapter with member x.Convert q = q
//        type BillAdapter() = interface QuoteAdapter with member x.Convert q = 1M - q

//        type Quote<'T> = string * DateTime * 'T // quote name * quote time * quote value (decimal / string / date ??)

        type QuoteName = Bid | Ask | Last | Vwap | Close | Mid | Custom

        type Quotes = { // todo to class
            Bid : decimal option
            Ask : decimal option
            Last : decimal option
            Vwap : decimal option
            Close : decimal option
            Mid : decimal option
            Custom : decimal option
        } with 
            member x.Recalculate () =
                let mid = 
                    match x.Bid, x.Ask with
                    | Some(bid), Some(ask) -> Some((bid+ask)/2M)
                    | _, Some(q) | Some(q), _ -> Some(q) // todo check for setting "calc mid if both quotes are present"
                    | _ -> None
                { x with Mid = mid }

            static member asList x = 
                [(Bid, x.Bid); (Ask, x.Ask); (Last, x.Last); (Vwap, x.Vwap); (Close, x.Close); (Mid, x.Mid); (Custom, x.Custom)]

            static member toList x = 
                Quotes.asList x 
                |> List.choose (function q, Some(d) -> Some(q, d) | _ -> None )

            static member toList (q1, q2) = 
                (Quotes.asList q1, Quotes.asList q2) ||> List.zip
                |> List.choose (fun ((qN1, d1), (qN2, d2)) -> 
                    match (d1, d2) with 
                    | Some (dv1), Some(dv2) when qN1 = qN2 -> 
                        Some (qN1, d1.Value, d2.Value) 
                    | _ -> None)

            static member empty = { Bid = None; Ask = None; Last = None; Vwap = None; Close = None; Mid = None; Custom = None }

        type Duration = Modified | Macauley | Effective
        type Spread = ISpread | ZSpread | AswSpread | OaSpread
        type YieldType = YTM | YTP | YTC | YTB | YTW | YTA
            
        type Result = Duration of Duration | Spread of Spread | Yield | Price | PD

        type Instrument = 
        | Bond of BondMetadata // * Quotes
        | Frn of FrnMetadata // * Quotes * Quotes
        | Irs of IrsMetadata // * Quotes * Quotes
        | Ccs of CcsMetadata
        | Ndf //of FwdMetadata
        | Fra of FraMetadata
        | Depo //of FwdMetadata
        | Cds

        type InstrumentRecord = { // todo to class
            Instrument : Instrument
            Weight : decimal
            Enabled : bool
        }
        
        type YesNoCan't = Yes | No | Can't

        type Approximation = // todo concrete cases
        | LinearInterpolation // default case
        | Regression // todo generalized regression using quotation expressions
        | Spline
        | IrModel

        type Group = { // todo to class ??
            Instruments : InstrumentRecord list
            Bootstrapped : YesNoCan't
            Interpolation : Approximation
        }

    module Features = 
        type BondFeature = Industry | Rating
        type Feature = Industry | SubIndustry | Rating 
        
//        // Базовое разбиение на на диапазоны
//        type FeatureConverter = 
//            abstract member Range : int * int
//            abstract member ToSlot : string -> int * string

    module Program = 
        open EikonDesktopDataAPI
        open Analytics
        open YieldMap.Loader.Calendar
        open YieldMap.Loader.SdkFactory
        open YieldMap.Loader.MetaChains
        open YieldMap.Loader.LiveQuotes

        type Main = {
            MetaChains : ChainMetaLoader 
            Quotes : Subscription
            Factory : EikonFactory
            DateTime : Calendar
        }

    module Ansamble = 
        open Analytics
        open YieldMap.Core.Calculations
        open Features
        open YieldMap.Tools.Aux.Workflows.Attempt

        type Gauge = Calc of Result | Given of Feature
        type Currency = Any | Concrete of string

        // Samples
        //    let chart1 = { X = Macauley |> Duration |> Calc; Y = Yield |> Calc; Currency = Concrete "USD" }
        //    let chart2 = { X = Effective |> Duration |> Calc; Y = ISpread |> Spread |> Calc; Currency = Concrete "RUB" }
        //    let chart3 = { X = Rating |> Given; Y = ZSpread |> Spread |> Calc; Currency = Any }
        type Axes = { X : Gauge; Y : Gauge; Currency : Currency }
            
        type PartySettings = {
            YieldMode : YieldType option
        }

        // todo VOLATILITY CURVES
        // todo FORWARD CURVES, IR CURVES, CURVE CALCULATORS, DEPO CURVES

        type Storage =  abstract member Get : string * Result -> decimal option

        type Party = {
            Settings : PartySettings
            Charts : Axes list
            Calculators : Calculator list
            Groups : Group list
        } and Calculator = abstract member Calculate : Party * Storage -> Storage 

        // 3) Нужно средство быстрого поиска данных по калькуляторам - по индивидуальной бумаге или группе бумаг. Или по полю!!!
        type BondYD = {
            Price : decimal
            Yield : Bonds.YieldInfo
            Duration : Bonds.DurationInfo
        }

        type BondQYD = Data of Map<QuoteName, BondYD>
            with 
                static member has (Data data) (quoteName, price) = data.ContainsKey quoteName && data.[quoteName].Price = price
                static member fetch (Data data) = data

        type BondStorage = 
            val storage : Map<string, BondQYD>

            new () = { storage = Map.empty }
            new (_storage) = { storage = _storage }

            member x.Storage = x.storage
            interface Storage with
                member x.Get (key, res) = None // todo!

        type StraightBondCalculator (m:Program.Main) = 
            let straight = Bonds.StraightCalc(m.Factory) 
            let frn = Bonds.FrnCalc(m.Factory) 

            let calcStraight quoteName price today settle (meta:Instruments.BondMetadata) = attempt {
                let yr = {
                    Bonds.StraightRequest.BS = meta.BondStructure
                    Bonds.StraightRequest.RS = meta.RateStructure
                    Bonds.StraightRequest.CPN = meta.Coupon
                    Bonds.StraightRequest.DT = today
                    Bonds.StraightRequest.MTY = meta.Maturity
                    Bonds.StraightRequest.STL = settle
                    Bonds.StraightRequest.PRC = Some price
                    Bonds.StraightRequest.YLD = None
                }
                let! yld = straight.Yield(yr) |> AsAttempt.value
                let dr = {yr with YLD = Some yld.Yield}
                let! dur = straight.Durations(dr) |> AsAttempt.value
                return {
                    Price = price
                    Yield = yld
                    Duration = dur
                }
            } 

            let calcFrn quoteName price currentIndex today settle (meta:Instruments.FrnMetadata) = attempt {
                let yr = {
                    Bonds.FrnRequest.FS = meta.FrnStructure
                    Bonds.FrnRequest.RS = meta.RateStructure
                    Bonds.FrnRequest.DT = today
                    Bonds.FrnRequest.MTY = meta.Maturity
                    Bonds.FrnRequest.STL = settle
                    Bonds.FrnRequest.PRC = Some price
                    Bonds.FrnRequest.YLD = None
                    Bonds.FrnRequest.CI = currentIndex
                    Bonds.FrnRequest.PI = currentIndex
                }
                let! yld = frn.Yield(yr) |> AsAttempt.value
                let dr = {yr with YLD = Some yld.Yield}
                let! dur = frn.Durations(dr) |> AsAttempt.value
                return {
                    Price = price
                    Yield = yld
                    Duration = dur
                }
            } 

            let evaluate (s:Map<string, BondQYD>) item today = 
                // For each instrument
                match item.Instrument with
                | Bond(meta) when item.Enabled -> // , quotes
                    // Evaluating usual bond
                    let s = // add ric to storage
                        if not <| s.ContainsKey meta.Ric then s.Add(meta.Ric, Map.empty |> Data) else s

                    let quotes = Quotes.empty // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    let settle = straight.Settle (today, meta.BondStructure)
                    let quotes = quotes |> Quotes.toList
                    (s, quotes) ||> List.fold (fun s (quoteName, price) ->
                        // For each quote
                        if not <| BondQYD.has s.[meta.Ric] (quoteName, price) then
                            let res = calcStraight quoteName price today settle meta  |> runAttempt 
                            match res with
                            | Some(bondYD) -> 
                                let rcrd = BondQYD.fetch s.[meta.Ric]
                                s.Add(meta.Ric, rcrd.Add(quoteName, bondYD) |> Data)
                            | None -> s
                        else s)  

                | Frn(meta) when item.Enabled ->  // , bondQuote, indexQuote
                    // Evaluating FRN

                    // I suggest that CurrentIndex = ProjectedIndex
                    let s = // add ric to storage
                        if not <| s.ContainsKey meta.Ric then s.Add(meta.Ric, Map.empty |> Data) else s
                    
                    let bondQuote = Quotes.empty // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    let indexQuote = Quotes.empty // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    let settle = frn.Settle (today, meta.FrnStructure)
                    let quotes = (bondQuote, indexQuote) |> Quotes.toList
                    (s, quotes) ||> List.fold (fun s (quoteName, bondPrice, indexPrice) ->
                        // For each quote
                        if not <| BondQYD.has s.[meta.Ric] (quoteName, bondPrice) then
                            let res = calcFrn quoteName bondPrice indexPrice today settle meta  |> runAttempt 
                            match res with
                            | Some(bondYD) -> 
                                let rcrd = BondQYD.fetch s.[meta.Ric]
                                s.Add(meta.Ric, rcrd.Add(quoteName, bondYD) |> Data)
                            | None -> s
                        else s) 

                | Irs(meta) -> s // , irsQuote, indexQuote
                | _ -> s

            interface Calculator with
                member x.Calculate (party, storage) =
                    let today = m.DateTime.Today
                    let s = (storage :?> BondStorage).Storage
                    let res = (s, party.Groups) ||> List.fold (fun s group ->
                        (s, group.Instruments) ||> List.fold (fun s item ->  evaluate s item today))
                    BondStorage(res) :> Storage