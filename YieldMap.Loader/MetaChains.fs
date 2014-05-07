namespace YieldMap.Loader.MetaChains

[<AutoOpen>]
module MetaChains = 
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
    
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.Requests
    open YieldMap.Loader.SdkFactory
    
    open YieldMap.Requests.MetaTables
    open YieldMap.Requests.Tools.Attrs
    open YieldMap.Requests.Tools.Converters

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Location

    let private logger = LogFactory.create "SdkFactory"

    /// Loads chains and metadata
    type ChainMetaLoader = 
        abstract member LoadChain : ChainRequest -> Async<Chain>
        abstract member LoadMetadata<'a when 'a : (new : unit -> 'a)> : rics:string array * ?timeout:int -> 'a Meta Async

    module private Watchers =
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
                        Chain.Answer(result)
                    else 
                        Chain.Failed(Exception("Invalid data format"))
                with e -> 
                    Chain.Failed(Exception(sprintf "Failed to parse chain, error is %s" <| e.ToString()))

            do chain.add_OnUpdate (
                fun status -> 
                    logger.TraceF "Chain / status -> %s" (status.ToString())
                    match status with 
                    | RT_DataStatus.RT_DS_FULL -> dataEvent.Trigger <| parseData chain.Data
                    | RT_DataStatus.RT_DS_PARTIAL -> () // todo logging
                    | _ -> dataEvent.Trigger <| Chain.Failed(Exception("Invalid ric"))
            )

            do chain.add_OnStatusChange (
                fun status ->
                    logger.TraceF "Status changed -> %s" (status.ToString())
                    match status with
                    | RT_SourceStatus.RT_SOURCE_UP -> ()
                    | _ -> dataEvent.Trigger <| Chain.Failed(Exception(sprintf "Invalid feed %s" chain.Source))
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
                |> Array.toList

            try
                let minRow = data.GetLowerBound(0)
                let maxRow = data.GetUpperBound(0)
                let minCol = data.GetLowerBound(1)
                let maxCol = data.GetUpperBound(1)

                let getConverter (conv : Type) = 
                    if converters.ContainsKey(conv) then converters.[conv] 
                    else 
                        let cnv = Activator.CreateInstance(conv) :?> Cnv
                        lock converters (fun () -> try converters.Add (conv, cnv) with :? ArgumentException -> ())
                        cnv

                let convert (value:obj) conv = 
                    let converter = getConverter conv
                    try converter.Convert <| value.ToString() with _ -> None

                let rec import acc n = 
                    if n > maxRow then
                        acc
                    else
                        try
                            let row = data.[n..n, *] |> Seq.cast<obj> |> Seq.toArray
                        
                            let res = new 'T() 
                            let t = typedefof<'T>

                            let rec importRow = function
                                | (num, name, converter) :: rest ->
                                    let p = t.GetProperty name
                                    
                                    // a kind of hack. It some item is out of bounds of returned array
                                    // it is considered to be null
                                    let value =  if num-minCol < Array.length row then row.[num-minCol] else null
                                    
                                    logger.TraceF "Converting value %A to type %s" value p.PropertyType.Name

                                    let convertedValue = 
                                        match converter with
                                        | Some conv -> try convert (value.ToString()) conv with _ -> None
                                        | None -> Some value

                                    match convertedValue with
                                    | Some v -> 
                                        p.SetValue(res, v) 
                                        importRow rest
                                    | None -> false
                                | [] -> true

                            if importRow fieldsInfo then
                                logger.Trace "Imported"
                                import ([res] @ acc) (n+1)
                            else 
                                logger.Warn "Row import failed"
                                import acc (n+1)

                        with e -> 
                            logger.WarnF "Failed to import row %A num %d because of %s" data.[n..n, *] n (e.ToString())
                            import acc (n+1)
            
                Meta.Answer <| import [] minRow
            with e -> Meta.Failed(e)

    module private MockOperations = 
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
                        | null -> return Chain.Failed <| Exception(sprintf "No chain %s in DB" setup.Ric)
                        | _ -> return Chain.Answer <| node.InnerText.Split('\t')
                    with e -> return Chain.Failed e
                } 
                |> Async.WithTimeoutEx (Some setup.Timeout)
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Chain.Failed e
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
                            return Meta.Answer []
                            //return Meta.Failed <| Exception ("No data")
                        
                    with e -> return Meta.Failed e
                } |> Async.WithTimeoutEx timeout

            async {
                try return! workflow
                with :? TimeoutException as e -> return Meta.Failed e
            }

    module private EikonOperations = 
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
                } 
                |> Async.WithTimeoutEx (Some setup.Timeout)
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Chain.Failed e
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
                } |> Async.WithTimeoutEx (Some timeout)

            async {
                try return! workflow
                with :? TimeoutException as e -> return Meta.Failed e
            }


    /// Connection : timeout;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : null
    type MockChainMeta(_today) =
        new () = MockChainMeta(defaultCalendar.Now)

        interface ChainMetaLoader with
            member x.LoadChain request = MockOperations.chain request (Some _today) 
            member x.LoadMetadata (rics, ?timeout) = 
                MockOperations.meta rics (Some _today) timeout

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type EikonChainMeta (factory:EikonFactory) =
        interface ChainMetaLoader with
            member x.LoadChain request = EikonOperations.chain (factory.CreateAdxRtChain ()) request
            member x.LoadMetadata (rics, ?timeout) = 
                match timeout with
                | Some time -> EikonOperations.meta (factory.CreateDex2Mgr ()) rics time
                | None -> EikonOperations.meta (factory.CreateDex2Mgr ()) rics 0

