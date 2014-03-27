namespace YieldMap.Loading

module LiveQuotes = 
    open EikonDesktopDataAPI
    open ThomsonReuters.Interop.RTX
    
    open System
    open System.Collections.Generic
    open System.Runtime.InteropServices
    open System.Threading

    open YieldMap.Logging
    open YieldMap.Tools
    open YieldMap.Tools.Disposer

    let private logger = LogFactory.create "LiveQuotes"

    type TimeoutAnswer<'T> = Timeout | Invalid of exn | Succeed of 'T 

    type RicFields = Map<string, string list>
    type FieldValue = Map<string, string>
    type RicFieldValue = Map<string, FieldValue>

    // useful extensions
    type AdxRtList with
        member x.Register (lst: (string*string list) list)  =
            match lst with
            | (ric, fields) :: rest -> 
                x.RegisterItems (ric, String.Join(",", fields))
                x.Register rest
            | [] -> ()

        member x.ExtractFv ric status = 
            try
                if set [RT_ItemStatus.RT_ITEM_DELAYED; RT_ItemStatus.RT_ITEM_OK] |> Set.contains status then
                    let data = x.ListFields(ric, RT_FieldRowView.RT_FRV_UPDATED, RT_FieldColumnView.RT_FCV_VALUE) :?> obj[,]
                    if data <> null then
                        let names =  data.[*, 0..0] |> Seq.cast<obj> |> Seq.map String.toString
                        let values = data.[*, 1..1] |> Seq.cast<obj> |> Seq.map String.toString
                        let fieldValues = (names, values) ||> Seq.zip |> Seq.filter (fun (_, v) -> v <> null) |> Map.ofSeq // or (cross (snd >> (<>)) null) 
                        Some fieldValues
                    else
                        logger.Warn <| sprintf "No data or ric %s" ric
                        None
                else 
                    logger.Warn <| sprintf "Ric %s has invalid status %A" ric status
                    None
            with e -> 
                logger.WarnEx "Failed to extract rfv" e
                None

        member x.RicsStatuses () = 
            let data = x.ListItems(RT_ItemRowView.RT_IRV_ALL, RT_ItemColumnView.RT_ICV_STATUS) :?> obj[,]

            let ricsList = data.[*, 0..0] |> Seq.cast<obj> |> Seq.map String.toString
            let statusList =  data.[*, 1..1] |> Seq.cast<obj> |> Seq.map (fun x -> x :?> RT_ItemStatus)
            Seq.zip ricsList statusList
    
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
                let onStatusChange listStatus sourceStatus runMode = 
                    this.Logger.Trace <| sprintf "OnStatusChange (%A, %A, %A)" listStatus sourceStatus runMode
                    if sourceStatus <> RT_SourceStatus.RT_SOURCE_UP then 
                        if listStatus = RT_ListStatus.RT_LIST_RUNNING then
                            snap.UnregisterAllItems ()
                        this.Logger.Warn <| sprintf "Source not up: %A" sourceStatus
                        this.Event.Trigger <| Invalid(exn(sprintf "Source not up: %A" sourceStatus))

                snap.add_OnStatusChange <| IAdxRtListEvents_OnStatusChangeEventHandler onStatusChange
                
                let onImage status =
                    match status with
                    | RT_DataStatus.RT_DS_FULL ->
                        let rec parseRics rics answer =
                            let update ric answer status =
                                this.Logger.Trace <| sprintf "Got ric %s with status %A" ric status
                                
                                match snap.ExtractFv ric status with
                                | Some fv -> Map.add ric fv answer
                                | None -> answer
                            try                                     
                                match rics with
                                | (ric, status) :: rest -> parseRics rest (update ric answer status)
                                | [] -> answer                   
                            with e -> 
                                logger.ErrorEx "failed " e
                                answer
                             
                        let result = parseRics (List.ofSeq <| snap.RicsStatuses()) Map.empty
                        this.Event.Trigger <| Succeed result
                    | RT_DataStatus.RT_DS_PARTIAL -> this.Logger.Debug "Partial data"
                    | _ -> this.Event.Trigger <| Invalid (exn(sprintf "Invalid data status: %A" status))

                snap.add_OnImage <| IAdxRtListEvents_OnImageEventHandler onImage
            
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
                        fields.remove_OnUpdate !_handler
                        let ricsLeft, results = handle allRics answers ric status
                        if Set.count ricsLeft = 0 then
                            _fieldsData.Trigger <| Succeed results 
                        else
                            _handler := (ricsLeft, results) ||> handler 
                            !_handler |> fields.add_OnUpdate))
                                
                _handler := handler (set rics) Map.empty
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

    type QuoteMode = OnTime of int | OnTimeIfUpdated of int | OnUpdate

    type EikonSubscription (_eikon:EikonDesktopDataAPI, _feed, _mode) = 
        inherit Disposer ()

        let requests = Map.empty
        let quotes = _eikon.CreateAdxRtList() :?> AdxRtList
        do 
            try quotes.Source <- _feed
            with :? COMException -> logger.Error "Failed to set up a source"

        let quotesRef = ref quotes
        let quotesEvent = Event<RicFieldValue>()

        let onTimeHandler = IAdxRtListEvents_OnTimeEventHandler (fun () -> 
            let rec parseRics rics answer =
                let update ric answer status =
                    logger.Trace <| sprintf "Got ric %s with status %A" ric status

                    if List.exists (fun (r, _) -> r = ric) rics then
                        match quotes.ExtractFv ric status with
                        | Some fieldValues -> Map.add ric fieldValues answer
                        | None -> answer
                    else 
                        logger.Warn <| sprintf "Ric %s was not requested" ric
                        answer
                try                                     
                    match rics with
                    | (ric, status) :: rest -> parseRics rest (update ric answer status)
                    | [] -> answer                   
                with e -> 
                    logger.ErrorEx "failed" e
                    answer
                             
            let result = parseRics (List.ofSeq <| quotes.RicsStatuses()) Map.empty
            quotesEvent.Trigger result
        )

        let onUpdateHandler = IAdxRtListEvents_OnUpdateEventHandler (fun ric _ status -> 
            logger.Trace <| sprintf "Got ric %s with status %A" ric status
            match quotes.ExtractFv ric status with
            | Some fieldValues -> quotesEvent.Trigger <| Map.add ric fieldValues Map.empty
            | None -> ()            
        )

        override x.DisposeManaged () = Ole32.killComObject quotesRef
        override x.DisposeUnmanaged () = ()

        interface Subscription with
            member x.OnQuotes = quotesEvent.Publish

            member x.Fields (rics, ?timeout) = 
                async {
                    try
                        let fields = _eikon.CreateAdxRtList() :?> AdxRtList
                        let fielder = ref fields
                        try
                            fields.Source <- _feed
                            let fieldWatcher = Watchers.FieldWatcher(rics, fields)
                            fields.RegisterItems (rics |> String.concat ",", "*")
                            fields.StartUpdates RT_RunMode.RT_MODE_ONUPDATE
                            let! res = Async.AwaitEvent fieldWatcher.FieldsData 
                            fields.CloseAllLinks ()
                            return res
                        finally
                            Ole32.killComObject fielder
                    with :? COMException as e ->
                        logger.ErrorEx "Failed to load fields" e
                        return Invalid e
                } |> Async.WithTimeoutEx timeout

            member x.Snapshot (ricFields, ?timeout) = 
                let rics, fields = Map.toList ricFields |> List.unzip
                let fields = List.concat fields |> set

                async {
                    try
                        let snap = _eikon.CreateAdxRtList() :?> AdxRtList
                        let snapshotter = ref snap
                        try
                            snap.Source <- _feed
                            snap.Register (Map.toList ricFields)

                            let snapWaiter = Watchers.SnapWatcher snap
                            snap.StartUpdates RT_RunMode.RT_MODE_IMAGE

                            let res = 
                                match timeout with
                                | Some time -> 
                                    try Async.RunSynchronously (Async.AwaitEvent snapWaiter.Snapshot, time)
                                    with :? TimeoutException -> Timeout
                                | None -> Async.AwaitEvent snapWaiter.Snapshot |> Async.RunSynchronously

                            snap.CloseAllLinks()
                            return res
                        finally
                            Ole32.killComObject snapshotter
                    with :? COMException as e ->
                        logger.ErrorEx "Failed to load fields" e
                        return Invalid e
                }

            member x.Start () = 
                try
                    quotes.remove_OnTime onTimeHandler
                    quotes.remove_OnUpdate onUpdateHandler

                    match _mode with
                    | OnTime interval ->
                        quotes.Mode <- sprintf "FRQ:%ds" interval
                        quotes.add_OnTime onTimeHandler
                        quotes.StartUpdates RT_RunMode.RT_MODE_ONTIME
                    | OnTimeIfUpdated interval ->
                        quotes.Mode <- sprintf "FRQ:%ds" interval
                        quotes.add_OnTime onTimeHandler
                        quotes.StartUpdates RT_RunMode.RT_MODE_ONTIME_IF_UPDATED
                    | OnUpdate -> 
                        quotes.Mode <- ""
                        quotes.add_OnUpdate onUpdateHandler
                        quotes.StartUpdates RT_RunMode.RT_MODE_ONUPDATE

                with :? COMException as e -> logger.WarnEx "Failed to start updates" e

            member x.Pause () = 
                try quotes.StopUpdates ()
                with :? COMException as e -> logger.WarnEx "Failed to pause updates" e

            member x.Stop () = 
                try
                    quotes.UnregisterAllItems ()
                    quotes.CloseAllLinks ()
                with :? COMException as e -> 
                    logger.WarnEx "Failed to stop updates" e

            member x.Add ricFields = 
                try quotes.Register (Map.toList ricFields)
                with :? COMException as e -> logger.WarnEx "Failed to add rics and fields" e

            member x.Remove rics =
                try quotes.UnregisterItems <| String.Join (",", rics)
                with :? COMException as e -> logger.WarnEx "Failed to remove rics" e

    type SlotItem = { Ric : string; Field : string; Value : string }
    type Slot = { Interval : float; Items : SlotItem list }
        with 
            static member toRfv items =
                let rec asRfv items agg = 
                    match items with
                    | { Ric=ric; Field=field; Value=value } :: rest -> 
                        let newFv = if agg |> Map.containsKey ric then agg.[ric] else Map.empty
                        asRfv rest (agg |> Map.add ric (newFv |> Map.add field value))
                    | [] -> agg
                asRfv items Map.empty

    [<AbstractClass>]
    type RfvGenerator() as this = 
        let stop = ref false
        let rfv = Event<RicFieldValue>()
        
        let rec flow () = async {
            for slot in this.Generate () do
                if !stop then return ()
                do! Async.Sleep (1000.0 * slot.Interval |> int)
                rfv.Trigger <| Slot.toRfv slot.Items

            if !stop then return ()
            else return! flow ()
        }

        do flow () |> Async.Start

        abstract member Generate : unit -> Slot seq
               
        member x.Rfv = rfv.Publish
        member x.Stop () = stop := true

    type SeqGenerator(slots, circular) = 
        inherit RfvGenerator()
        override x.Generate () = seq {
            for slot in slots -> slot
            if circular then yield! x.Generate ()
        }

    // todo Random Generator // do I need it? for stress tests mostly yes
//    type RndGenerator(slots, circular) = 
//        inherit RfvGenerator()
//        override x.Generate () = seq {
//            for slot in slots -> slot
//            if circular then yield! x.Generate ()
//        }

    // todo mocks!
    type MockSubscription(generator : RfvGenerator) =
        inherit Disposer ()
            
        let quotesEvent = Event<RicFieldValue>()
        let paused = ref false
        let ricsAndFields = Dictionary()

        let eventFilter (rfv : RicFieldValue) = 
            let rec filterOut items agg =
                match items with
                | (ric, fieldValues) :: rest -> 
                    if ricsAndFields.ContainsKey ric then
                        let newFieldValues = fieldValues |> Map.filter (fun fieldName _ -> ricsAndFields.[ric] |> Set.contains fieldName)
                        filterOut rest (agg |> Map.add ric newFieldValues)
                    else filterOut rest agg
                | [] -> agg

            if !paused then Map.empty
            else 
                let items = lock ricsAndFields (fun _ -> 
                    filterOut (Map.toList rfv) Map.empty)
                items |> Map.filter (fun _ value -> not <| Map.isEmpty value)

        do 
            generator.Rfv 
            |> Observable.map eventFilter 
            |> Observable.filter (fun x -> not <| Map.isEmpty x) 
            |> Observable.add (fun x -> quotesEvent.Trigger x)
            
        override x.DisposeManaged () = ()
        override x.DisposeUnmanaged () = generator.Stop ()

        interface Subscription with
            member x.OnQuotes = quotesEvent.Publish
            member x.Fields (rics, ?timeout) = async { return Timeout }
            member x.Snapshot (ricFields, ?timeout) = async { return Timeout }
            member x.Start () = paused := false
            member x.Pause () = paused := true

            member x.Stop () = 
                lock ricsAndFields (fun () -> ricsAndFields.Clear()) 
                (x :> Subscription).Pause()

            member x.Add ricFields = 
                let rec add items = 
                    match items with
                    | (ric, fields) :: rest -> 
                        lock ricsAndFields (fun () ->
                            if ricsAndFields.ContainsKey ric then 
                                ricsAndFields.[ric] <- ricsAndFields.[ric] + set fields
                            else ricsAndFields.Add (ric, set fields))
                        add rest
                    | [] -> ()
                ricFields |> Map.toList |> add

            member x.Remove rics =
                let rec remove items =
                    match items with 
                    | ric :: rest -> 
                        lock ricsAndFields (fun () -> ricsAndFields.Remove ric |> ignore)
                        remove rest
                    | [] -> ()
                remove rics

    // todo API's!