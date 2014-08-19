namespace YieldMap.Loader.MetaChains

[<AutoOpen>]
module MetaChains = 
    open System
    open System.Collections.Generic
    open System.IO
    open System.Reflection
    open System.Runtime.InteropServices
    open System.Xml

    open Dex2
    open ThomsonReuters.Interop.RTX
    
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.SdkFactory
    
    open YieldMap.Requests
    open YieldMap.Requests.Converters
    open YieldMap.Requests.Attributes

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Response
    open YieldMap.Tools.Logging
    open YieldMap.Tools.Location

    let private logger = LogFactory.create "MetaChains"

    /// Loads chains and metadata
    type ChainMetaLoader = 
        abstract member LoadChain : ChainRequest -> string [] Tweet Async
        abstract member LoadMetadata<'a when 'a : (new : unit -> 'a)> 
            : rics:string array * ?timeout:int -> 'a list Tweet Async

    module private Watchers =
        /// Event wrapper of async call
        type RawMeta = obj[,] Tweet // Data of obj[,] | Failed of string

        type Meta<'T when 'T : (new : unit -> 'T)> (loader:RData) =
            let dataEvent = new Event<_>()
            do loader.add_OnUpdate (
                fun (status:DEX2_DataStatus) _ -> 
                    match status with 
                    | DEX2_DataStatus.DE_DS_PARTIAL                           -> failwith "Unexpected partial data"
                    | DEX2_DataStatus.DE_DS_FULL when loader.Data = null      -> dataEvent.Trigger (RawMeta.Failure <| Problem "No data")
                    | DEX2_DataStatus.DE_DS_FULL when (loader.Data :? obj[,]) -> dataEvent.Trigger (RawMeta.Answer (loader.Data :?> obj[,]))
                    | DEX2_DataStatus.DE_DS_FULL                              -> dataEvent.Trigger (RawMeta.Failure <| Problem "Invalid data format")
                    | _                                                       -> dataEvent.Trigger (RawMeta.Failure <| Problem (status.ToString()))
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
                        Tweet.Answer result
                    else 
                        Tweet.Failure (Failure.Problem "Invalid data format")
                with e -> 
                    Tweet.Failure (Failure.Error e)

            do chain.add_OnUpdate (
                fun status -> 
                    logger.TraceF "Chain / status -> %s" (status.ToString())
                    match status with 
                    | RT_DataStatus.RT_DS_FULL -> dataEvent.Trigger <| parseData chain.Data
                    | RT_DataStatus.RT_DS_PARTIAL -> () // todo logging
                    | _ -> dataEvent.Trigger <| Tweet.Failure (Failure.Problem "Invalid ric")
            )

            do chain.add_OnStatusChange (
                fun status ->
                    logger.TraceF "Status changed -> %s" (status.ToString())
                    match status with
                    | RT_SourceStatus.RT_SOURCE_UP -> ()
                    | _ -> dataEvent.Trigger <| Tweet.Failure (Failure.Problem (sprintf "Invalid feed %s" chain.Source))
            )

            member __.Data = dataEvent.Publish

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
                    try converter.Convert <| value.ToString() with e -> Invalid (e.Message)

                let import2 () =
                    let ans = Stack<'T> ()
                    for n = minRow to maxRow do
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
                                    
                                logger.TraceF "Converting value %A field %s to type %s" value p.Name p.PropertyType.Name

                                let convertedValue = 
                                    match converter with
                                    | Some conv -> convert (value.ToString()) conv
                                    | None -> Product value

                                match convertedValue with
                                | Product v -> 
                                    p.SetValue(res, v) 
                                    importRow rest
                                | Empty ->
                                    p.SetValue(res, null) 
                                    importRow rest
                                | Invalid reason -> Some <| sprintf "[%s] in %s error: %s" (value.ToString()) (p.Name) reason
                            | [] -> None

                            match importRow fieldsInfo with
                            | Some failure ->
                                logger.WarnF "Import failed: %s" failure
                                
                            | None -> 
                                logger.Trace "Imported"
                                ans.Push res

                        with e -> 
                            logger.WarnF "Failed to import row %A num %d because of %s" data.[n..n, *] n (e.ToString())
                    
                    ans.ToArray()

                import2 () 
                |> List.ofArray 
                |> Tweet.Answer 

            with e -> Tweet.Failure (Failure.Error e)

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
                        xDoc.Load path
                        let node = xDoc.SelectSingleNode(sprintf "/chains/chain[@name='%s']" setup.Ric)
                        match node with
                        | null -> return Failure <| Problem (sprintf "No chain %s in DB" setup.Ric)
                        | _ -> 
                            let res = node.InnerText.Split('\t') |> Array.filter (not << String.IsNullOrWhiteSpace)
                            return Answer res
                    with e -> return Failure (Error e)
                } 
                |> Async.WithTimeoutEx (Some setup.Timeout)
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Failure Timeout
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
                            return MetaParser.parse<'a> data
                        else
                            return Answer []
                        
                    with e -> return Failure (Error e)
                } |> Async.WithTimeoutEx timeout

            async {
                try return! workflow
                with :? TimeoutException as e -> return Failure Timeout
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
                    with :? COMException -> return Failure (Problem "Not connected to Eikon")
                } 
                |> Async.WithTimeoutEx (Some setup.Timeout)
            
            async {
                try return! workflow
                with :? TimeoutException as e -> return Failure Timeout
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
                    | Answer arr -> return MetaParser.parse<'a> arr
                    | Failure e -> return Failure e
                with :? COMException -> return Failure (Problem "Not connected to Eikon")
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
                with :? TimeoutException as e -> return Failure Timeout
            }

    /// Connection : timeout;
    /// Today / LoadChain / LoadData : mock;
    /// Adfin calcs : null
    type MockChainMeta(c:Calendar) =
        interface ChainMetaLoader with
            member __.LoadChain request =
                async {
                    try return! MockOperations.chain request (Some c.Today) 
                    with e -> return Failure (Error e)
                }
            member __.LoadMetadata (rics, ?timeout) = 
                async {
                    try return! MockOperations.meta rics (Some c.Today) timeout
                    with e -> return Failure (Error e)
                }

    /// Connection : Eikon;
    /// Today / LoadChain / LoadData : Real;
    /// Adfin calcs : Eikon
    type EikonChainMeta (factory:EikonFactory) =
        interface ChainMetaLoader with
            member __.LoadChain request = EikonOperations.chain (factory.CreateAdxRtChain ()) request
            member __.LoadMetadata (rics, ?timeout) = 
                async {
                    try
                        return!
                            match timeout with
                            | Some time -> EikonOperations.meta (factory.CreateDex2Mgr ()) rics time
                            | None -> EikonOperations.meta (factory.CreateDex2Mgr ()) rics 0
                    with e -> return Failure (Error e)
                }