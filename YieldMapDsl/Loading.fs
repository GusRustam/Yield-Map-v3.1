namespace YieldMap.Loading

module HistoricalData = 
    type HistoryLoader = class end

module LiveQuotes = 
    open EikonDesktopDataAPI
    open ThomsonReuters.Interop.RTX
    
    open System
    open System.Runtime.InteropServices
    open System.Threading

    open YieldMap.Tools
    open YieldMap.Tools.Disposer
    open YieldMap.Tools.Logging

    let private logger = LogFactory.create "LiveQuotes"

    type TimeoutAnswer<'T> = Timeout | Invalid of exn | Succeed of 'T 

    type RicFields = Map<string, string list>
    type FieldValue = Map<string, string>
    type RicFieldValue = Map<string, FieldValue>
    
    module private Watchers = 
        [<AbstractClass>]
        type Watcher (list : AdxRtList) =
            let _evt = Event<_>()
            let _lock = obj()
            let mutable _failed = false
            let _logger = LogFactory.create "Watcher"
            do 
                let onStatusChange  listStatus sourceStatus runMode = 
                    _logger.Trace <| sprintf "OnStatusChange (%A, %A, %A)" listStatus sourceStatus runMode
                    lock _lock (fun _ ->
                        if sourceStatus <> RT_SourceStatus.RT_SOURCE_UP then 
                            if listStatus = RT_ListStatus.RT_LIST_RUNNING then
                                list.UnregisterAllItems ()
                            _failed <- true
                            _logger.Warn <| sprintf "Source not up: %A" sourceStatus
                            _evt.Trigger <| Invalid(exn(sprintf "Source not up: %A" sourceStatus)))

                list.add_OnStatusChange <| IAdxRtListEvents_OnStatusChangeEventHandler onStatusChange

            member x.Logger = _logger
            member x.Failed = _failed
            member x.Event = _evt

        type SnapWatcher(snap : AdxRtList) as this = 
            inherit Watcher (snap)

            do 
                let onStatusChange  listStatus sourceStatus runMode = 
                    this.Logger.Trace <| sprintf "OnStatusChange (%A, %A, %A)" listStatus sourceStatus runMode
                    if sourceStatus <> RT_SourceStatus.RT_SOURCE_UP then 
                        if listStatus = RT_ListStatus.RT_LIST_RUNNING then
                            snap.UnregisterAllItems ()
                        this.Logger.Warn <| sprintf "Source not up: %A" sourceStatus
                        this.Event.Trigger <| Invalid(exn(sprintf "Source not up: %A" sourceStatus))

                snap.add_OnStatusChange <| IAdxRtListEvents_OnStatusChangeEventHandler onStatusChange

                snap.add_OnImage (fun status -> 
                    match status with
                    | RT_DataStatus.RT_DS_FULL ->
                        let data = snap.ListItems(RT_ItemRowView.RT_IRV_ALL, RT_ItemColumnView.RT_ICV_STATUS) :?> obj[,]

                        let slice (data : obj[,]) col = 
                            let rec getLine num arr =
                                if num < data.GetLength(0) then getLine (num+1) (data.[num, col] :: arr) else arr
                            getLine 0 [] 

                        let ricsList = slice data 0 |> List.map (fun x -> x.ToString())
                        let statusList = slice data 1 |> List.map (fun x -> x :?> RT_ItemStatus)
                        let ricsAndStatuses = List.zip ricsList statusList

                        let rec parseRics rics answer =
                            let update ric answer status =
                                this.Logger.Trace <| sprintf "Got ric %s with status %A" ric status

                                if List.exists (fun (r, _) -> r = ric) rics then
                                    if set [RT_ItemStatus.RT_ITEM_DELAYED; RT_ItemStatus.RT_ITEM_OK] |> Set.contains status then
                                        let data = snap.ListFields(ric, RT_FieldRowView.RT_FRV_UPDATED, RT_FieldColumnView.RT_FCV_VALUE) :?> obj[,]
                                        let names = slice data 0 |> List.map (fun x -> x.ToString())
                                        let values = slice data 1 |> List.map (fun x -> x.ToString())
                                        let fieldValues = (names, values) ||> List.zip |> Map.ofList
                            
                                        Map.add ric fieldValues answer
                                    else 
                                        this.Logger.Warn <| sprintf "Ric %s has invalid status %A" ric status
                                        answer
                                else 
                                    this.Logger.Warn <| sprintf "Ric %s was not requested" ric
                                    answer
                                                                 
                            match rics with
                            | (ric, status) :: rest -> parseRics rest (update ric answer status)
                            | [] -> answer                   
                             
                        let result = parseRics ricsAndStatuses Map.empty
                        this.Event.Trigger <| Succeed result
                    | RT_DataStatus.RT_DS_PARTIAL -> this.Logger.Debug "Partial data"
                    | _ -> this.Event.Trigger <| Invalid (exn(sprintf "Invalid data status: %A" status)))
            
            member x.Snapshot = this.Event.Publish
            

        type FieldWatcher (rics : string list, fields : AdxRtList) = 
            let _lock = obj()
            let _fieldsData = Event<_>()
            let _rics = set rics
            
            let _logger = LogFactory.create "FieldWatcher"
            
            let mutable _failed = false
            let _handler = ref null

            do
                let onStatusChange  listStatus sourceStatus runMode = 
                    _logger.Trace <| sprintf "OnStatusChange (%A, %A, %A)" listStatus sourceStatus runMode

                    if sourceStatus <> RT_SourceStatus.RT_SOURCE_UP then 
                        if listStatus = RT_ListStatus.RT_LIST_RUNNING then
                            fields.UnregisterAllItems ()
                        _logger.Warn <| sprintf "Source not up: %A" sourceStatus
                        _failed <- true
                        _fieldsData.Trigger <| Invalid(exn(sprintf "Source not up: %A" sourceStatus))

                fields.add_OnStatusChange <| IAdxRtListEvents_OnStatusChangeEventHandler onStatusChange

                let handle allRics answers ric status =
                    let rec getLine num arr (data : obj[,]) =
                        if num < data.GetLength(0) then getLine (num+1) ((data.[num, 0].ToString()) :: arr) data else arr

                    if Set.contains ric allRics && not _failed then
                        _logger.Trace <| sprintf "Got ric %s with status %A" ric status
                        if set [RT_ItemStatus.RT_ITEM_DELAYED; RT_ItemStatus.RT_ITEM_OK] |> Set.contains status then
                            let data = fields.ListFields(ric, RT_FieldRowView.RT_FRV_UPDATED, RT_FieldColumnView.RT_FCV_STATUS) :?> obj[,]
                            let f = data |> getLine 0 [] 
                            
                            Set.remove ric allRics, Map.add ric f answers
                        else 
                            _logger.Warn <| sprintf "Ric %s has invalid status %A" ric status
                            allRics, answers
                    else 
                        _logger.Trace <| sprintf "Ric %s was not requested" ric
                        allRics, answers

                
                let rec handler allRics answers = IAdxRtListEvents_OnUpdateEventHandler (fun ric _ status ->
                    lock _lock (fun _ -> 
                        fields.remove_OnUpdate <| !_handler
                        let ricsLeft, results = handle allRics answers ric status
                        if Set.count ricsLeft = 0 then
                            _fieldsData.Trigger <| Succeed results 
                        else
                            _handler := (ricsLeft, results) ||> handler 
                            !_handler |> fields.add_OnUpdate))
                                
                _handler :=  handler (set rics) Map.empty
                fields.add_OnUpdate !_handler

            member x.FieldsData = _fieldsData.Publish

    type Subscription = 
        abstract member OnQuotes : RicFieldValue IEvent

        abstract member Fields : string list * int option -> RicFields TimeoutAnswer Async
        abstract member Snapshot : RicFields * int option -> RicFieldValue TimeoutAnswer Async

        abstract member Start : unit -> unit
        abstract member Pause : unit -> unit
        abstract member Stop : unit -> unit

        abstract member Add : RicFields -> unit
        abstract member Remove : string list -> unit // removes rics

    type EikonSubscription (_eikon:EikonDesktopDataAPI, _feed) = 
        inherit Disposer ()

        let eikon = _eikon
        let requests = Map.empty
        let quotes = ref (eikon.CreateAdxRtList() :?> AdxRtList)
        let quotesEvent = Event<RicFieldValue>()

        override x.DisposeManaged () = Ole32.killComObject quotes
        override x.DisposeUnmanaged () = ()

        interface Subscription with
            member x.OnQuotes = quotesEvent.Publish

            member x.Fields (rics, ?timeout) = 
                async {
                    try
                        let fields = _eikon.CreateAdxRtList() :?> AdxRtList
                        fields.Source <- _feed
                        let fieldWatcher = Watchers.FieldWatcher(rics, fields)
                        fields.RegisterItems (rics |> String.concat ",", "*")
                        fields.StartUpdates RT_RunMode.RT_MODE_ONUPDATE
                        let! res = Async.AwaitEvent fieldWatcher.FieldsData 
                        fields.CloseAllLinks ()
                        return res
                    with :? COMException as e ->
                        logger.ErrorEx "Failed to load fields" e
                        return Invalid e
                } |> Async.WithTimeoutEx timeout

            member x.Snapshot (ricFields, ?timeout) = 
                let rics, fields = Map.toList ricFields |> List.unzip
                let fields = List.concat fields |> set

                async {
                    try
                        let snap = eikon.CreateAdxRtList() :?> AdxRtList
                        let snapshotter = ref snap
                        try
                            snap.Source <- _feed
                            snap.RegisterItems(String.Join(",", rics), String.Join(",", fields))
                            let snapWaiter = Watchers.SnapWatcher(snap)
                            snap.StartUpdates RT_RunMode.RT_MODE_IMAGE
                            let! res = Async.AwaitEvent snapWaiter.Snapshot
                            snap.CloseAllLinks()
                            return res
                        finally
                            Ole32.killComObject snapshotter
                    with :? COMException as e ->
                        logger.ErrorEx "Failed to load fields" e
                        return Invalid e
                }

            member x.Start () = ()
            member x.Pause () = ()
            member x.Stop () = ()
            member x.Add _ = ()
            member x.Remove _ = ()

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
    
    open YieldMap.Requests
    open YieldMap.Requests.Answers
    open YieldMap.Tools
    open YieldMap.Tools.Logging

    let private logger = LogFactory.create "SdkFactory"

    module private Watchers =
        type Eikon(eikon : EikonDesktopDataAPI) =
            let changed = new Event<_>()
            do eikon.add_OnStatusChanged (fun e -> 
                logger.Trace <| sprintf "Status changed -> %A!" e
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
                    status.ToString() |>  sprintf "Chain / status -> %s" |> logger.Trace
                    match status with 
                    | RT_DataStatus.RT_DS_FULL -> dataEvent.Trigger <| parseData chain.Data
                    | RT_DataStatus.RT_DS_PARTIAL -> () // todo logging
                    | _ -> dataEvent.Trigger <| Answers.Chain.Failed(Exception("Invalid ric"))
            )

            do chain.add_OnStatusChange (
                fun status ->
                    status.ToString() |>  sprintf "Status changed -> %s" |> logger.Trace
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
                                let p = t.GetProperty(name)

                                let converted value = 
                                    match conv with
                                    | Some(conv) -> 
                                        logger.Trace <| sprintf "Converting value %A to type %s" value p.PropertyType.Name

                                        let converter = 
                                            if converters.ContainsKey(conv) then 
                                                converters.[conv] 
                                            else 
                                                let cnv = Activator.CreateInstance(conv) :?> Cnv
                                                lock converters (fun () -> try converters.Add(conv, cnv) with :? ArgumentException -> ())
                                                cnv

                                        try converter.Convert <| value.ToString()
                                        with _ -> value
                                    | _ -> 
                                        logger.Trace <| sprintf "Value is %A" value
                                        value
                            
                                p.SetValue(res, converted row.[num-minCol]) 
                            import ([res] @ acc) (n+1)
                        with e -> 
                            logger.Debug <| sprintf "Failed to import row %A num %d because of %s" data.[n..n, *] n (e.ToString())
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
                        logger.Trace <| sprintf "The path to load chains is %s" path
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
                        logger.Trace <| sprintf "The path to load meta is %s" path
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

    module DateChangeTrigger = 
        let waitForTime (evt:DateTime Event) (span : TimeSpan) = 
            let rec wait (time : DateTime) =  async {
                do! Async.Sleep(1000)
                let now = DateTime.Now
                if now - time >= span then
                    try
                        evt.Trigger DateTime.Today
                    with e -> logger.Error <| sprintf "Failed to handle date change: %s" (e.ToString())
                    return! wait now
                else return! wait time
            }

            wait DateTime.Now

        let waitForTomorrow (evt:DateTime Event) = 
            let rec wait (time : DateTime) =  async {
                do! Async.Sleep(1000) // midnight check once a second
                let now = DateTime.Now
                if now.Date <> time.Date then
                    try
                        evt.Trigger DateTime.Today // tomorrow has come, informing
                    with e -> logger.Error <| sprintf "Failed to handle date change: %s" (e.ToString())
                    return! wait now // and now we'll count from new time, current's today
                else return! wait time // counting from recent today
            }

            wait DateTime.Now

    /// In real case date m
    /// Connects to Eikon, stores current date
    type Loader = 
        abstract member Today : unit -> DateTime
        abstract member DateChanged : DateTime IEvent

        abstract member Connect : unit -> Async<Answers.Connection>

        abstract member LoadChain : ChainRequest -> Async<Answers.Chain>
        abstract member LoadMetadata<'a when 'a : (new : unit -> 'a)> : string array -> int option -> Async<Answers.Meta<'a>>
        
        abstract member CreateAdxBondModule : unit -> AdxBondModule
        abstract member CreateAdxRtChain : unit -> AdxRtChain
        abstract member CreateDex2Mgr : unit -> Dex2Mgr

    /// Connection : timeout;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : null
    type MockOnlyFactory(_today) =
        let today = _today
        let dateChanged = Event<DateTime>()

        new () = MockOnlyFactory(DateTime.Today)

        interface Loader with
            member x.Today () = today
            member x.DateChanged = dateChanged.Publish
            member x.Connect () = MockOperations.connect ()
            member x.LoadChain request = MockOperations.chain request (Some today) 
            member x.LoadMetadata rics timeout = MockOperations.meta rics (Some today) timeout
            member x.CreateAdxBondModule () = null
            member x.CreateAdxRtChain () = null
            member x.CreateDex2Mgr () = null

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : Eikon
    type TestEikonFactory(_eikon, _today)  =
        let today = _today
        let eikon = _eikon
        let dateChanged = Event<DateTime>()

        new (_eikon) = TestEikonFactory(_eikon, DateTime.Today)

        interface Loader with
            member x.Connect () = EikonOperations.connect eikon
            member x.DateChanged = dateChanged.Publish
            member x.Today () = today
            member x.LoadChain request = MockOperations.chain request (Some today) 
            member x.LoadMetadata rics timeout = MockOperations.meta rics (Some today) timeout
            member x.CreateAdxBondModule () = eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateDex2Mgr () = eikon.CreateDex2Mgr() :?> Dex2Mgr

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type OuterEikonFactory (eikon:EikonDesktopDataAPI) =
        let dateChanged = Event<DateTime>()
        do DateChangeTrigger.waitForTomorrow dateChanged |> Async.Start

        interface Loader with
            member x.Connect () = EikonOperations.connect eikon
            member x.DateChanged = dateChanged.Publish
            member x.Today () = DateTime.Today
            member x.LoadChain request = EikonOperations.chain ((x :> Loader).CreateAdxRtChain ()) request
            member x.LoadMetadata rics timeout = EikonOperations.meta ((x :> Loader).CreateDex2Mgr ()) rics timeout
            member x.CreateAdxBondModule () = eikon.CreateAdxBondModule() :?> AdxBondModule
            member x.CreateAdxRtChain () = eikon.CreateAdxRtChain() :?> AdxRtChain
            member x.CreateDex2Mgr () = eikon.CreateDex2Mgr() :?> Dex2Mgr


    /// Connection : timeout;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type InnerEikonFactory (eikon:EikonDesktopDataAPI) =
        let dateChanged = Event<DateTime>()
        do DateChangeTrigger.waitForTomorrow dateChanged |> Async.Start

        interface Loader with
            member x.Connect () = MockOperations.connect ()
            member x.DateChanged = dateChanged.Publish
            member x.Today () = DateTime.Today
            member x.LoadChain request = EikonOperations.chain ((x :> Loader).CreateAdxRtChain ()) request
            member x.LoadMetadata rics timeout = EikonOperations.meta ((x :> Loader).CreateDex2Mgr ()) rics timeout
            member x.CreateAdxBondModule () = AdxBondModuleClass() :> AdxBondModule
            member x.CreateAdxRtChain () = new AdxRtChainClass() :> AdxRtChain
            member x.CreateDex2Mgr () =  new Dex2MgrClass() :> Dex2Mgr