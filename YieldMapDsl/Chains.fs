namespace YieldMap.Loading

module HistoricalData = 
    type HistoryLoader = class end

module SdkFactory = 
    open System
    open System.Collections.Generic
    open System.IO
    open System.Reflection
    open System.Runtime.InteropServices
    open System.Xml

    open AdfinXAnalyticsFunctions
    open Dex2
    open EikonDesktopDataAPI
    open ThomsonReuters.Interop.RTX
    
    open YieldMap.Calendar
    open YieldMap.Logging
    open YieldMap.Requests
    open YieldMap.Requests.Answers
    open YieldMap.Tools

    let private logger = LogFactory.create "SdkFactory"

    module private Watchers =
        type Eikon(eikon : EikonDesktopDataAPI) =
            let changed = new Event<_>()
            do eikon.add_OnStatusChanged (fun e -> 
                logger.TraceF "Status changed -> %A!" e
                changed.Trigger <| Answers.Connection.parse e)
            member self.StatusChanged = changed.Publish

        /// Event wrapper of async call
        type RawMeta = Data of obj[,] | Failed of string

        type Meta<'T when 'T : (new : unit -> 'T)> (loader:RData) =
            let dataEvent = new Event<_>()
            do loader.add_OnUpdate (
                fun (status:DEX2_DataStatus) _ -> 
                    match status with 
                    | DEX2_DataStatus.DE_DS_PARTIAL                           -> () // ??
                    | DEX2_DataStatus.DE_DS_FULL when loader.Data = null      -> dataEvent.Trigger <| Failed("No data")
                    | DEX2_DataStatus.DE_DS_FULL when (loader.Data :? obj[,]) -> dataEvent.Trigger <| Data(loader.Data :?> obj[,])
                    | DEX2_DataStatus.DE_DS_FULL                              -> dataEvent.Trigger <| Failed("Invalid data format")
                    | _                                                       -> dataEvent.Trigger <| Failed(status.ToString())
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
                    logger.TraceF "Chain / status -> %s" (status.ToString())
                    match status with 
                    | RT_DataStatus.RT_DS_FULL -> dataEvent.Trigger <| parseData chain.Data
                    | RT_DataStatus.RT_DS_PARTIAL -> () // todo logging
                    | _ -> dataEvent.Trigger <| Answers.Chain.Failed(Exception("Invalid ric"))
            )

            do chain.add_OnStatusChange (
                fun status ->
                    logger.TraceF "Status changed -> %s" (status.ToString())
                    match status with
                    | RT_SourceStatus.RT_SOURCE_UP -> ()
                    | _ -> dataEvent.Trigger <| Answers.Chain.Failed(Exception(sprintf "Invalid feed %s" chain.Source))
            )

            member x.Data = dataEvent.Publish

    module private MetaParser = 
        let private converters = Dictionary()

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
                        try
                            let row = data.[n..n, *] |> Seq.cast<obj> |> Seq.toArray
                        
                            let res = new 'T() 
                            let t = typedefof<'T>

                            for (num, name, conv) in fieldsInfo do
                                let p = t.GetProperty name

                                let converted value = 
                                    match conv with
                                    | Some(conv) -> 
                                        logger.TraceF "Converting value %A to type %s" value p.PropertyType.Name

                                        let converter = 
                                            if converters.ContainsKey(conv) then 
                                                converters.[conv] 
                                            else 
                                                let cnv = Activator.CreateInstance(conv) :?> Cnv
                                                lock converters (fun () -> try converters.Add (conv, cnv) with :? ArgumentException -> ())
                                                cnv

                                        try converter.Convert <| value.ToString()
                                        with _ -> value
                                    | _ -> 
                                        logger.TraceF "Value is %A" value
                                        value
                            
                                p.SetValue(res, converted row.[num-minCol]) 
                            import ([res] @ acc) (n+1)
                        with e -> 
                            logger.DebugF "Failed to import row %A num %d because of %s" data.[n..n, *] n (e.ToString())
                            import acc (n+1)
            
                Meta.Answer <| import [] minRow
            with e -> Meta.Failed(e)

    module private MockOperations = 
        let connect () = async {
            do! Async.Sleep(500) 
            return Answers.Connected
        }

        let private metaPath<'a> (date : DateTime option) = 
            match date with
            | None -> sprintf "data/meta/%s.csv" <| typedefof<'a>.Name
            | Some dt -> sprintf "data/meta/%s/%s.csv" <|| (dt.ToString("ddMMyyyy"), typedefof<'a>.Name)

        let private chainPath (date : DateTime option) =
            match date with
            | None -> "data/chains/chains.xml" 
            | Some dt -> sprintf "data/chains/%s/chains.xml" <|dt.ToString("ddMMyyyy")

        let chain setup date = 
            let workflow = 
                async {
                    do! Async.Sleep(500) 
                    try
                        let xDoc = XmlDocument()
                        let path = Path.Combine(Location.path, chainPath date)
                        logger.TraceF "The path to load chains is %s" path
                        xDoc.Load(path)
                        let node = xDoc.SelectSingleNode(sprintf "/chains/chain[@name='%s']" setup.Ric)
                        match node with
                        | null -> return Answers.Chain.Failed <| Exception(sprintf "No chain %s in DB" setup.Ric)
                        | _ -> return Answers.Chain.Answer <| node.InnerText.Split('\t')
                    with e -> return Answers.Chain.Failed e
                } |> Async.WithTimeoutEx setup.Timeout
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Answers.Chain.Failed e
            }

        let meta<'a when 'a : (new : unit -> 'a)> rics date timeout = 
            let workflow = 
                async {
                    do! Async.Sleep(500) 
                    // TODO delayed rics handling
                    try
                        let setRics = set rics
                        let path = Path.Combine(Location.path, metaPath<'a> date)
                        logger.TraceF "The path to load meta is %s" path
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
                            let data = Array2D.init rows cols (fun r c -> box items.[r].[c])
                            let x = MetaParser.parse<'a> data
                            return x
                        else
                            return Answers.Meta.Failed <| Exception ("No data")
                        
                    with e -> return Answers.Meta.Failed e
                } |> Async.WithTimeoutEx timeout

            async {
                try return! workflow
                with :? TimeoutException as e -> return Answers.Meta.Failed e
            }

    module private EikonOperations = 
        let connect (eikon : EikonDesktopDataAPI) = 
            let watcher = Watchers.Eikon eikon
            let res = eikon.Initialize()
            Async.AwaitEvent watcher.StatusChanged

        let chain (adxRtChain:AdxRtChain) setup = 
            let workflow = 
                async {
                    try
                        adxRtChain.Source <- setup.Feed
                        adxRtChain.Mode <- setup.Mode
                        adxRtChain.ItemName <- setup.Ric
                        let evts = Watchers.Chain(adxRtChain)
                        adxRtChain.RequestChain()
                
                        return! Async.AwaitEvent evts.Data
                    with :? COMException -> return Chain.Failed <| Exception "Not connected to Eikon"
                } |> Async.WithTimeoutEx setup.Timeout
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Answers.Chain.Failed e
            }

        let meta<'a when 'a : (new : unit -> 'a)> (dex2 : Dex2Mgr) (rics:string array) timeout = 
            let doMeta (mgr:RData) setup = async {
                try
                    mgr.DisplayParam <- setup.Display
                    mgr.RequestParam <- setup.Request
                    mgr.FieldList <- String.Join (",", setup.Fields)
                    mgr.InstrumentIDList <- String.Join (",", rics)

                    let evts = Watchers.Meta<'a> mgr
    
                    mgr.Subscribe(false)
                    let! data = Async.AwaitEvent evts.Data
                    match data with
                    | Watchers.Data arr -> return MetaParser.parse<'a> arr
                    | Watchers.Failed e -> return Meta.Failed <| Exception(e)
                with :? COMException -> return Meta.Failed <| Exception "Not connected to Eikon"
            }

            let workflow = 
                async {
                    let cookie = dex2.Initialize()
                    let mgr = dex2.CreateRData(cookie)

                    let setup = MetaRequest.extract<'a> ()

                    return! doMeta mgr setup
                } |> Async.WithTimeoutEx timeout

            async {
                try return! workflow
                with :? TimeoutException as e -> return Answers.Meta.Failed e
            }

    /// Connects to Eikon, loads chains and metadata
    type Loader = 
        abstract member Connect : unit -> Async<Answers.Connection>
        abstract member LoadChain : ChainRequest -> Async<Answers.Chain>
        abstract member LoadMetadata<'a when 'a : (new : unit -> 'a)> : string array -> int option -> Async<Answers.Meta<'a>>
        abstract member CreateAdxBondModule : unit -> AdxBondModule
        abstract member CreateAdxRtChain : unit -> AdxRtChain
        abstract member CreateAdxRtList : unit -> AdxRtList
        abstract member CreateDex2Mgr : unit -> Dex2Mgr

    /// Connection : timeout;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : null
    type MockOnlyFactory(_today) =
        new () = MockOnlyFactory(defaultCalendar.Now)

        interface Loader with
            member x.Connect () = MockOperations.connect ()
            member x.LoadChain request = MockOperations.chain request (Some _today) 
            member x.LoadMetadata rics timeout = MockOperations.meta rics (Some _today) timeout
            member x.CreateAdxBondModule () = null
            member x.CreateAdxRtChain () = null
            member x.CreateAdxRtList () = null
            member x.CreateDex2Mgr () = null

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : Eikon
    type TestEikonFactory(_eikon, _today)  =
        let dateChanged = Event<DateTime>()
        new (_eikon) = TestEikonFactory(_eikon, defaultCalendar.Today)

        interface Loader with
            member x.Connect () = EikonOperations.connect _eikon
            member x.LoadChain request = MockOperations.chain request (Some _today) 
            member x.LoadMetadata rics timeout = MockOperations.meta rics (Some _today) timeout
            member x.CreateAdxBondModule () = _eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = _eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateAdxRtList () = _eikon.CreateAdxRtList() :?> AdxRtList
            member x.CreateDex2Mgr () = _eikon.CreateDex2Mgr() :?> Dex2Mgr

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type OuterEikonFactory (eikon:EikonDesktopDataAPI) =
        interface Loader with
            member x.Connect () = EikonOperations.connect eikon
            member x.LoadChain request = EikonOperations.chain ((x :> Loader).CreateAdxRtChain ()) request
            member x.LoadMetadata rics timeout = EikonOperations.meta ((x :> Loader).CreateDex2Mgr ()) rics timeout
            member x.CreateAdxBondModule () = eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateAdxRtList () = eikon.CreateAdxRtList() :?> AdxRtList
            member x.CreateDex2Mgr () = eikon.CreateDex2Mgr() :?> Dex2Mgr


    /// Connection : timeout;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type InnerEikonFactory (eikon:EikonDesktopDataAPI) =
        interface Loader with
            member x.Connect () = MockOperations.connect ()
            member x.LoadChain request = EikonOperations.chain ((x :> Loader).CreateAdxRtChain ()) request
            member x.LoadMetadata rics timeout = EikonOperations.meta ((x :> Loader).CreateDex2Mgr ()) rics timeout
            member x.CreateAdxBondModule () = AdxBondModuleClass() :> AdxBondModule
            member x.CreateAdxRtChain () = new AdxRtChainClass() :> AdxRtChain
            member x.CreateAdxRtList () = new AdxRtListClass() :> AdxRtList
            member x.CreateDex2Mgr () =  new Dex2MgrClass() :> Dex2Mgr