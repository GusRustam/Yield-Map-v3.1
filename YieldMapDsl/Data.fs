namespace YieldMap.Data

open YieldMap.Tools   
 
open Dex2
open EikonDesktopDataAPI
open ThomsonReuters.Interop.RTX
open AdfinXAnalyticsFunctions

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Globalization
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open System.Xml

module AdfinCreate = 
    type Adfin = 
        abstract member CreateAdxBondModule : unit -> AdxBondModule

    type EikonFactory (eikon:EikonDesktopDataAPI) =
        interface Adfin with
            member x.CreateAdxBondModule () = eikon.CreateAdxBondModule() :?> AdxBondModule

    type GeneralFactory () =
        interface Adfin with
            member x.CreateAdxBondModule () = AdxBondModuleClass() :> AdxBondModule


module Requests =
    type ChainRequest = { Feed : string; Mode : string; Ric : string }

    let private (|FirstLetter|) (str:string) = 
        if String.IsNullOrEmpty(str) then String.Empty 
        else string(str.[0]).ToUpper()

    (* Converters *)
    type Cnv = abstract member Convert : string -> obj

    type BoolConverter() = 
        interface Cnv with
            member self.Convert x = 
                match x with
                | FirstLetter "Y" -> true 
                | _ -> false
                :> obj

    type DateConverter() = 
        interface Cnv with
            member self.Convert x = 
                let success, date = 
                    DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.None)
                if success then date :> obj else x :> obj // чтобы работало и рейтеровскими данными и с моими

    (* Request attributes *) 
    type RequestAttribute = 
        inherit Attribute
        val Request : string
        val Display : string
        new (display) = {Request = String.Empty; Display = display}
        new (request, display) = {Request = request; Display = display}

    type FieldAttribute = 
        inherit Attribute
        val Order : int
        val Name : string
        val Converter : Type option 
    
        override self.ToString () = 
            let convName (cnv : Type option) =  
                match cnv with
                | Some(tp) -> tp.Name 
                | None -> "None"
            sprintf "%d | %s | %s" self.Order self.Name (convName self.Converter)

        new(order) = {Order = order; Name = String.Empty; Converter = None}
        new(order, name) = {Order = order; Name = name; Converter = None}
        new(order, name, converter) = {Order = order; Name = name; Converter = Some(converter)}

    (* Tools *)
    /// Parameters to make a request
    type MetaRequest = {
        Fields : string list
        Display : string
        Request : string
    } with 
        static member empty = { Fields = []; Display = ""; Request = "" }
        /// Creates MetaSetup object and some structure to parse and store data into T easily 
        static member extract<'T> () = 
            let def = typedefof<'T>
            match def.Attr<RequestAttribute>() with
            | Some(x) -> 
                let fields = 
                    def.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                    |> Array.map (fun p -> p.Attr<FieldAttribute>())
                    |> Array.choose (function | Some(x) when not <| String.IsNullOrEmpty(x.Name) -> Some(x.Name) | _ -> None)
                    |> List.ofArray
                {Request = x.Request; Display = x.Display;  Fields = fields}
            | None -> failwith "Invalid setup, no RequestAttribute"
    
module Answers = 
    open Requests

    type Connection = Connected | Failed of exn
        with static member parse (e:EEikonStatus) = 
                match e with
                | EEikonStatus.Connected -> Connected
                | _ -> Failed(Exception(e.ToString()))


    type Meta = Answer of obj[,] | Failed of exn
    type Chain = Answer of string array | Failed of exn

[<RequireQualifiedAccess>]
module Parser =
    open Requests
    type Meta<'T> = Answer of 'T | Failed of exn
    let parse<'T when 'T : (new : unit -> 'T)> (data:obj[,])  =
        let fieldsInfo =
            // Array of pairs : order * variable name * variable type
            typedefof<'T>.GetProperties(BindingFlags.Instance ||| BindingFlags.Public) 
            |> Array.choose (fun p -> 
                match p.Attr<FieldAttribute>() with
                | Some(attr) -> Some(attr.Order, p.Name, attr.Converter)
                | _ -> None)

        try
            let minRow = data.GetLowerBound(0)
            let maxRow = data.GetUpperBound(0)
            let minCol = data.GetLowerBound(1)
            let maxCol = data.GetUpperBound(1)

            let rec import acc n = 
                if n > maxRow then
                    acc
                else
                    let row = data.[n..n, *] |> Seq.cast<obj> |> Seq.toArray
                        
                    let res = new 'T() 
                    let t = typedefof<'T>

                    // todo same reflection on every line; memoize it somehow; maybe add PropertyInfo into parsing, and converter too.
                    for (num, name, conv) in fieldsInfo do
                        let p = t.GetProperty(name)

                        let converted value = 
                            match conv with
                            | Some(conv) -> 
                                let converter = Activator.CreateInstance(conv : Type) :?> Cnv
                                try converter.Convert <| value.ToString()
                                with _ -> value
                            | _ -> value
                            
                        p.SetValue(res, converted row.[num-minCol]) // <- kinda mutability 
                        
                    import ([res] @ acc) (n+1)

            let res = import [] minRow
            Answer(res)
        with e -> Failed(e)

[<RequireQualifiedAccess>]
module Watchers =
    type Eikon(eikon : EikonDesktopDataAPI) =
        let changed = new Event<_>()
        do eikon.add_OnStatusChanged (fun e -> changed.Trigger <| Answers.Connection.parse e)
        member self.StatusChanged = changed.Publish

        /// Event wrapper of async call
    type Meta<'T when 'T : (new : unit -> 'T)> (loader:RData) =
        let dataEvent = new Event<_>()
        do loader.add_OnUpdate (
            fun (status:DEX2_DataStatus) _ -> 
                match status with 
                | DEX2_DataStatus.DE_DS_PARTIAL                           -> () // ??
                | DEX2_DataStatus.DE_DS_FULL when loader.Data = null      -> dataEvent.Trigger <| Answers.Meta.Failed(Exception("No data"))
                | DEX2_DataStatus.DE_DS_FULL when (loader.Data :? obj[,]) -> dataEvent.Trigger <| Answers.Meta.Answer(loader.Data :?> obj[,])
                | DEX2_DataStatus.DE_DS_FULL                              -> dataEvent.Trigger <| Answers.Meta.Failed(Exception("Invalid data format"))
                | _                                                       -> dataEvent.Trigger <| Answers.Meta.Failed(Exception(status.ToString()))
        )
        member self.Data = dataEvent.Publish

    type Chain(chain:AdxRtChain) =
        let dataEvent = new Event<_>()

        let parseData (data:obj) = 
            try
                if data :? Array then
                    let data = data :?> Array
                    let min = data.GetLowerBound(0)
                    let max = data.GetUpperBound(0)
                    let result  = Array.create (max - min  + 1) String.Empty

                    for index in min .. max do // mutability!
                        result.[index-min] <- data.GetValue(index).ToString()
                        
                    let result = Array.filter (not << String.IsNullOrEmpty) result
                    Answers.Chain.Answer(result)
                else 
                    Answers.Chain.Failed(Exception("Invalid data format"))
            with e -> 
                Answers.Chain.Failed(Exception(sprintf "Failed to parse chain, error is %s" <| e.ToString()))

        do chain.add_OnUpdate (
            fun status -> 
                printfn "Status -> %s" <| status.ToString()
                match status with 
                | RT_DataStatus.RT_DS_FULL -> dataEvent.Trigger <| parseData chain.Data
                | RT_DataStatus.RT_DS_PARTIAL -> () // todo logging
                | _ -> dataEvent.Trigger <| Answers.Chain.Failed(Exception("Invalid ric"))
        )

        do chain.add_OnStatusChange (
            fun status ->
                printfn "Status changed -> %s" <| status.ToString()
                match status with
                | RT_SourceStatus.RT_SOURCE_UP -> ()
                | _ -> dataEvent.Trigger <| Answers.Chain.Failed(Exception(sprintf "Invalid feed %s" chain.Source))
        )

        member x.Data = dataEvent.Publish

module Calculations = 
    open AdfinXAnalyticsFunctions
    open YieldMap.Tools.Workflows
    open YieldMap.Tools.Workflows.Attempt

    [<RequireQualifiedAccess>]
    module Bonds = 
        type YieldToWhat = 
            YTM | YTP | YTC | YTB | YTW | YTA | Perpetual
            static member tryParse (s:string) =
                match s.ToUpper().Trim() with
                | "YTM" | "Maturity" -> Some(YTM)
                | "YTP" | "Put" -> Some(YTP)
                | "YTC" | "Call" -> Some(YTC)
                | "YTB" -> Some(YTB)
                | "YTW" -> Some(YTW)
                | "YTA" -> Some(YTA)
                | "Perpetual" -> Some(Perpetual)
                | _ -> None
            
        // todo spread duration, key rate duration
        // todo yield, duration -> another currency !!!!

        type StraightRequest = {
            BS : string           // bond structure
            RS : string           // rate structure
            DT : DateTime         // calc date
            CPN : decimal         // coupon rate
            MTY : DateTime option // maturity date; supports perpetuals
            STL : int             // settle date
            
            PRC : decimal option  // price
            YLD : decimal option  // yield
        } with 
            member r.Yield = r.YLD |> asAttempt
            member r.Price = r.PRC |> asAttempt

        type FrnRequest = {
            FS : string           // bond structure
            RS : string           // rate structure
            DT : DateTime         // calc date
            MTY : DateTime option // maturity date; supports perpetuals
            STL : int             // settle date

            CI : decimal          // current index
            PI : decimal          // project index
            
            PRC : decimal option  // price
            YLD : decimal option  // yield
        } with 
            member r.Yield = r.YLD |> asAttempt
            member r.Price = r.PRC |> asAttempt

        type YieldInfo = {
            Yield : decimal
            ToWhat : YieldToWhat
            ToDate : DateTime option
        }

        type DurationInfo = {
            Effective : decimal
            Macauley : decimal
            Modified : decimal
            AverageLife : decimal option // perpetuals do not have average life
            Convexity : decimal
            PVPB : decimal
        }

        //---------------
        let getValue str f = attempt {
            let (success, res) = f str
            if not success then return! fail
            return res
        }
        let getFloat str = getValue str Double.TryParse
        let getDecimal str = getValue str Decimal.TryParse
        let getInt str =  getValue str Int32.TryParse
        let getYtw str = asAttempt (YieldToWhat.tryParse str)
        let getNotNull v = attempt {
            if v = null then return! fail
            return v
        }
        //---------------

        type GroupCalc = 
            abstract member Bootstrap : unit -> unit
            abstract member Interpolate : unit -> unit
            abstract member ToXY : unit -> unit

        /// Handles straight, putable/callable (european) and perpetual bonds
        type StraightCalc (factory : AdfinCreate.Adfin) = 
            let bondModule = factory.CreateAdxBondModule()

//            abstract member PointSpread : SpreadRequest * Curve -> decimal option
//            abstract member ZSpread : SpreadRequest * Curve -> decimal option
//            abstract member AswSpread : SpreadRequest * Curve -> decimal option
//            abstract member OaSpread : SpreadRequest * Curve -> decimal option

            member x.Settle (d, s) = bondModule.BdSettle(d, s)
            member x.Yield (yr:StraightRequest) = 
                try
                    attempt {
                        let matDate = match yr.MTY with Some(x) -> box x | None -> null
                        let! p = yr.Price
                        let! yields = bondModule.AdBondYield(yr.STL, float p, matDate, float yr.CPN, yr.BS, yr.RS, "") :?> Array |> getNotNull
                        let rowNum = yields.GetLowerBound(0)

                        let! rate = yields.GetValue(rowNum, 1).ToString() |> getDecimal
                        let! serial = yields.GetValue(rowNum, 2).ToString() |> getInt

                        let toDate = ExcelDates.toDateTime(serial)
                        let! toWhat = yields.GetValue(rowNum, 4).ToString() |> getYtw

                        return {
                            Yield = rate
                            ToWhat = toWhat
                            ToDate = Some(toDate)
                        }
                    } |> runAttempt
                with _ -> None

            member x.Price (pr:StraightRequest) = 
                try
                    attempt {
                        let! y = pr.Yield
                        let matDate = match pr.MTY with Some(x) -> box x | None -> null

                        let res = bondModule.AdBondPrice(pr.STL, float y, matDate, float pr.CPN, 0.0, pr.BS, pr.RS, "", "") :?> Array
                        return! res.GetValue(1, 1).ToString() |> getDecimal
                    } |> runAttempt
                with _ -> None
                
            member x.Durations (dr:StraightRequest) = 
                try
                    attempt {
                        let! y = dr.Yield
                        let matDate = match dr.MTY with Some(x) -> box x | None -> null
                    
                        let! derivatives = bondModule.AdBondDeriv(dr.STL, y, matDate, float dr.CPN, 0.0, dr.BS, dr.RS, "", "") :?> Array |> getNotNull
                            
                        let rowNum = derivatives.GetLowerBound(0)
                        let! modDur = derivatives.GetValue(rowNum, 3).ToString() |> getDecimal
                        let! pvbp = derivatives.GetValue(rowNum, 4).ToString() |> getDecimal
                        let! macDur = derivatives.GetValue(rowNum, 5).ToString() |> getDecimal
                        let avgLife = derivatives.GetValue(rowNum, 6).ToString() |> getDecimal |> runAttempt // !!
                        let! cvxty = derivatives.GetValue(rowNum, 7).ToString() |> getDecimal

                        let price delta = x.Price {dr with YLD = Some (y + delta)} |> asAttempt
                        
                        let delta = 1e-4M
                        let! pMinus = price -delta
                        let! pPlus = price delta
                        let effDur = (pMinus - pPlus) / 2.0M * delta

                        return {
                            Effective = decimal effDur
                            Macauley = decimal macDur
                            Modified = decimal modDur
                            AverageLife = avgLife
                            Convexity = cvxty
                            PVPB = pvbp
                        }
                    } |> runAttempt
                with _ -> None

        type FrnCalc(factory : AdfinCreate.Adfin) = 
            let bondModule = factory.CreateAdxBondModule()
            member x.Settle (d, s) = bondModule.BdSettle(d, s)
            member x.Yield fr = 
                try
                    attempt {
                        let matDate = match fr.MTY with Some(x) -> box x | None -> null
                        let! p = fr.Price
                        let! yields = bondModule.AdFrnYield(fr.STL, float p, matDate, 0.0, fr.CI, fr.PI, fr.FS, fr.RS, "", "") :?> Array |> getNotNull
                        let rowNum = yields.GetLowerBound(0)

                        let! rate = yields.GetValue(rowNum, 1).ToString() |> getDecimal
                        let! serial = yields.GetValue(rowNum, 2).ToString() |> getInt

                        let toDate = ExcelDates.toDateTime(serial)
                        let! toWhat = yields.GetValue(rowNum, 4).ToString() |> getYtw

                        return {
                            Yield = rate
                            ToWhat = toWhat
                            ToDate = Some(toDate)
                        }
                    } |> runAttempt
                with _ -> None

            member x.Price (pr:FrnRequest) = 
                try
                    attempt {
                        let! y = pr.Yield
                        let matDate = match pr.MTY with Some(x) -> box x | None -> null

                        let res = bondModule.AdFrnPrice(pr.STL, float y, matDate, 0.0, 0.0, float pr.CI, pr.PI, pr.FS, pr.RS, "", "") :?> Array
                        return! res.GetValue(1, 1).ToString() |> getDecimal
                    } |> runAttempt
                with _ -> None

            member x.Durations (dr:FrnRequest) = 
                try
                    attempt {
                        let matDate = match dr.MTY with Some(x) -> box x | None -> null
                        let! y = dr.Yield
                    
                        let! derivatives = bondModule.AdFrnDeriv(dr.STL, y, matDate, 0.0, 0.0, dr.CI, dr.PI, dr.FS, dr.RS, "", "") :?> Array |> getNotNull
                        // AdBondDeriv returns 
                        // 1. Price
                        // 2. OptionFreePrice
                        // 3. Spread Duration
                        // 4. Index Duration
                        // 5. PVBP
                        // 6. Duration
                        // 7. Average Life
                        // 8. Convexity
                        // 9. YTW/YTB Date

                        let rowNum = derivatives.GetLowerBound(0)
                        let! modDur = derivatives.GetValue(rowNum, 4).ToString() |> getDecimal // todo Index Duration is true IR sensitivity!
                        let! pvbp = derivatives.GetValue(rowNum, 5).ToString() |> getDecimal
                        let! macDur = derivatives.GetValue(rowNum, 6).ToString() |> getDecimal
                        let avgLife = derivatives.GetValue(rowNum, 7).ToString() |> getDecimal |> runAttempt
                        let! cvxty = derivatives.GetValue(rowNum, 8).ToString() |> getDecimal

                        let price delta = x.Price {dr with YLD = Some (y + delta)} |> asAttempt
                      
                        let delta = 1e-4M
                        let! pMinus = price -delta
                        let! pPlus = price delta
                        let effDur = (pMinus - pPlus) / 2.0M * delta

                        return {
                            Effective = decimal effDur
                            Macauley = decimal macDur
                            Modified = decimal modDur
                            AverageLife = avgLife
                            Convexity = cvxty
                            PVPB = pvbp
                        }
                    } |> runAttempt
                with _ -> None
   
module Loading =
    open Requests

    type HistoryLoader = class end
    type LiveLoader = class end

    type MetaLoader = 
        abstract member Connect : unit -> Async<Answers.Connection>
        abstract member LoadChain : ChainRequest -> Async<Answers.Chain>
        abstract member LoadMetadata<'a when 'a : (new : unit -> 'a)> : string array -> Async<Answers.Meta>
//        abstract member LoadHistory : HistorySetup -> Async<DataResult>
//        abstract member StartRealtime : RealtimeSetup -> Async<DataResult> // todo wut if i'd like to stop loading or change subscription?!!
   

    module Loader =
        let chain (adxRtChain:AdxRtChain) setup = async {
            try
                adxRtChain.Source <- setup.Feed
                adxRtChain.Mode <- setup.Mode
                adxRtChain.ItemName <- setup.Ric
                let evts = Watchers.Chain(adxRtChain)
                adxRtChain.RequestChain()
                
                return! Async.AwaitEvent evts.Data
            with :? COMException -> return Answers.Chain.Failed(Exception "Not connected to Eikon")
        }

        let meta<'a when 'a : (new : unit -> 'a)> (mgr:RData) setup (rics:string array) = async {
            try
                mgr.DisplayParam <- setup.Display
                mgr.RequestParam <- setup.Request
                mgr.FieldList <-  String.Join (",", setup.Fields)
                mgr.InstrumentIDList <- String.Join (",", rics)

                let evts = Watchers.Meta<'a> mgr
    
                mgr.Subscribe(false)
                return! Async.AwaitEvent evts.Data
            with :? COMException -> return Answers.Meta.Failed(Exception "Not connected to Eikon")
        }

    // todo API
    type MockLoader =
        val chainFileName : string
        val metaFileName : string
        val date : DateTime

        new () = { 
            chainFileName = "chains" 
            metaFileName = "meta"
            date = DateTime.Today
        }

        new (_date) = {
            chainFileName = "chains" 
            metaFileName = "meta"
            date = _date
        }

        new (_chainFileName, _metaFileName, _date) = { 
            chainFileName = _chainFileName 
            metaFileName = _metaFileName
            date = _date
        }

        member private self.MetaPath<'a> () = printfn "%s_%s_%s.xml" self.metaFileName <| typedefof<'a>.Name <| self.date.ToString("ddMMyyyy")

        interface MetaLoader with
            member self.Connect () = async {
                do! Async.Sleep(500) 
                return Answers.Connected
            }

            member self.LoadChain setup = async {
                do! Async.Sleep(500) 
                try
                    let xDoc = XmlDocument()
                    xDoc.Load(Path.Combine(Location.path, self.chainFileName + ".xml"))
                    let node = xDoc.SelectSingleNode(sprintf "/chains/chain[@name='%s']" setup.Ric)
                    match node with
                    | null -> return Answers.Chain.Failed <| Exception(sprintf "No chain %s in DB" setup.Ric)
                    | _ -> return Answers.Chain.Answer <| node.InnerText.Split(' ')
                with e -> return Answers.Chain.Failed e
            }

            member self.LoadMetadata<'a when 'a : (new : unit -> 'a)> rics = async {
                do! Async.Sleep(500) 
                try
                    let setRics = set rics
                    let path = Path.Combine(Location.path, self.metaFileName + ".xml")
                    let items = 
                        path 
                        |> File.ReadLines
                        |> Seq.map (fun line -> line.Split('\t'))
                        |> Seq.filter (fun arr -> Array.length arr > 0)
                        |> Seq.choose (fun arr -> if setRics.Contains (arr.[0].Trim()) then Some arr else None)
                        |> Array.ofSeq

                    if Array.length items > 0 then
                        let rows = Array.length items
                        let cols = Array.length items.[0]
                        return Array2D.init rows cols (fun r c -> box items.[r].[c]) |> Answers.Meta.Answer
                    else
                        return Array2D.zeroCreate 0 0 |> Answers.Meta.Answer
                        
                with e -> return Answers.Meta.Failed e
            }


    type OuterLoader(eikon : EikonDesktopDataAPI) = 
        interface MetaLoader with
            member self.Connect () = async {
                let watcher = Watchers.Eikon eikon
                eikon.Initialize() |> ignore
                return! Async.AwaitEvent watcher.StatusChanged
            }

            member self.LoadChain setup = async {
                let adxRtChain = eikon.CreateAdxRtChain() :?> AdxRtChain
                return! Loader.chain adxRtChain setup
            }

            member self.LoadMetadata<'a when 'a : (new : unit -> 'a)> rics = async {
                let dex2 = eikon.CreateDex2Mgr() :?> Dex2Mgr
                let cookie = dex2.Initialize()
                let mgr = dex2.CreateRData(cookie)

                let setup = MetaRequest.extract<'a> ()

                return! Loader.meta<'a> mgr setup rics
            }

    type InnerLoader() = 
        interface MetaLoader with
            member self.Connect () = async {
                do! Async.Sleep(500) 
                return Answers.Connected
            }

            member self.LoadChain setup = async {
                let adxRtChain = AdxRtChainClass()
                return! Loader.chain adxRtChain setup
            }

            member self.LoadMetadata<'a when 'a : (new : unit -> 'a)> rics = async {
                let dex2 = Dex2MgrClass()
                let cookie = dex2.Initialize()
                let mgr = dex2.CreateRData(cookie)

                let setup = MetaRequest.extract<'a> ()

                return! Loader.meta<'a> mgr setup rics
            }
        
module Time =
    type TimeProvider = abstract member Today : DateTime
    type CurrentTimeProvider () = interface TimeProvider with member x.Today = DateTime.Today
    type FixedTimeProvider(d) = interface TimeProvider with member x.Today = d

module MetaTables = 
    open Requests

    [<Request("RH:In")>]
    type BondDescr() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.X.ADF_BondStructure")>] 
        member val BondStructure = String.Empty with get, set

        [<Field(2, "EJV.X.ADF_RateStructure")>] 
        member val RateStructure = String.Empty with get, set

        [<Field(3, "EJV.C.Description")>] 
        member val Description = String.Empty with get, set

        [<Field(4, "EJV.C.OriginalAmountIssued")>] 
        member val OriginalAmountIssued : float Nullable = Nullable() with get, set

        [<Field(5, "EJV.C.IssuerName")>] 
        member val IssuerName = String.Empty with get, set

        [<Field(6, "EJV.C.BorrowerName")>] 
        member val BorrowerName = String.Empty with get, set

        [<Field(7, "EJV.X.ADF_Coupon")>] 
        member val Coupon : float Nullable = Nullable() with get, set

        [<Field(8, "EJV.C.IssueDate")>] 
        member val IssueDate : DateTime Nullable = Nullable() with get, set

        [<Field(9, "EJV.C.MaturityDate")>] 
        member val MaturityDate : DateTime Nullable = Nullable() with get, set

        [<Field(10, "EJV.C.Currency")>] 
        member val Currency = String.Empty with get, set

        [<Field(11, "EJV.C.ShortName")>] 
        member val ShortName = String.Empty with get, set

        [<Field(12, "EJV.C.IsCallable", typeof<BoolConverter>)>] 
        member val IsCallable = false with get, set

        [<Field(13, "EJV.C.IsPutable", typeof<BoolConverter>)>] 
        member val IsPutable = false with get, set

        [<Field(14, "EJV.C.IsFloater", typeof<BoolConverter>)>] 
        member val IsFloater = false with get, set

        [<Field(15, "EJV.C.IsConvertible", typeof<BoolConverter>)>] 
        member val IsConvertible = false with get, set

        [<Field(16, "EJV.C.IsStraight", typeof<BoolConverter>)>] 
        member val IsStraight = false with get, set

        [<Field(17, "EJV.C.Ticker")>] 
        member val Ticker = String.Empty with get, set

        [<Field(18, "EJV.C.Series")>] 
        member val Series = String.Empty with get, set

        [<Field(19, "EJV.C.BorrowerCntyCode")>] 
        member val BorrowerCountry = String.Empty with get, set

        [<Field(20, "EJV.C.IssuerCountry")>] 
        member val IssuerCountry = String.Empty with get, set

        [<Field(21, "RI.ID.ISIN")>]
        member val Isin = String.Empty with get, set

        [<Field(22, "EJV.C.ParentTicker")>]
        member val ParentTicker = String.Empty with get, set

        [<Field(23, "EJV.C.SeniorityTypeDescription")>]
        member val SeniorityType = String.Empty with get, set

        [<Field(24, "EJV.C.SPIndustryDescription")>]
        member val Industry = String.Empty with get, set

        [<Field(25, "EJV.C.SPIndustrySubDescription")>]
        member val SubIndustry = String.Empty with get, set

        [<Field(26, "EJV.C.InstrumentTypeDescription")>]
        member val Instrument = String.Empty with get, set

    [<Request("D:1984;2020", "RH:In;D")>]
    type CouponData() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1)>]
        member val Date : DateTime Nullable = Nullable() with get, set

        [<Field(2, "EJV.C.CouponRate")>]
        member val Rate = 0.0 with get, set

    [<Request("RTS:FDL;SPI;MIS;MDL;FIS RTSC:FRN", "RH:In")>] 
    type IssueRatingData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.IR.Rating")>]
        member val Rating = String.Empty with get, set

        [<Field(2, "EJV.IR.RatingDate")>]
        member val RatingDate : DateTime Nullable = Nullable() with get, set

        [<Field(3, "EJV.IR.RatingSourceCode")>]
        member val RatingSourceCode = String.Empty with get, set

    [<Request("RTSRC:S&P;MDY;FTC", "RH:In")>]
    type IssuerRatingData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.GR.Rating")>]
        member val Rating = String.Empty with get, set

        [<Field(2, "EJV.GR.RatingDate")>]
        member val RatingDate : DateTime Nullable = Nullable() with get, set

        [<Field(3, "EJV.GR.RatingSourceCode")>]
        member val RatingSourceCode = String.Empty with get, set

    [<Request("RH:In")>]
    type FrnData() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.X.FRNFLOOR")>]
        member val Floor : float Nullable = Nullable() with get, set

        [<Field(2, "EJV.X.FRNCAP")>]
        member val Cap : float Nullable = Nullable() with get, set

        [<Field(3, "EJV.X.FREQ")>]
        member val Frequency = String.Empty with get, set

        [<Field(4, "EJV.X.ADF_MARGIN")>]
        member val Margin : float Nullable = Nullable() with get, set

    [<Request("RH:In;Con")>]
    type RicData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1)>]
        member val Contributor = String.Empty with get, set

        [<Field(2, "EJV.C.RICS")>]
        member val ContributedRic = String.Empty with get, set