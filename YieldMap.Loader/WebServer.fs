namespace YieldMap.Loader.WebServer

[<AutoOpen>]
module WebServer = 
    open Newtonsoft.Json
    open System
    open YieldMap.Tools.Aux

    type ApiQuote() =
        member val Ric = String.Empty with get, set
        member val Field = String.Empty with get, set
        member val Value = String.Empty with get, set
        override x.ToString() = sprintf "<%s | %s | %s>" x.Ric x.Field x.Value
        static member create ric field value = ApiQuote(Ric = ric, Field = field, Value = value)

    type ApiQuotes() = 
        member val Quotes : ApiQuote array = Array.empty with get, set
        override x.ToString() = sprintf "%A" x.Quotes
        static member create arr = ApiQuotes(Quotes = arr)
        static member pack (q : ApiQuotes) = q |> JsonConvert.SerializeObject |> String.toBytes
        static member unpack b = JsonConvert.DeserializeObject<ApiQuotes>(String.fromBytes b)
        static member toRfv (apiQuotes : ApiQuotes) = 
            apiQuotes.Quotes 
            |> Array.fold (fun agg item -> 
                let fv = if agg |> Map.containsKey item.Ric then agg.[item.Ric] else Map.empty
                let fv = fv |> Map.add item.Field item.Value
                agg |> Map.add item.Ric fv
            ) Map.empty 

module ApiServer = 
    open Newtonsoft.Json

    open System
    open System.Net
    open System.Text
    open System.IO

    open YieldMap.Tools.Logging
    open YieldMap.Tools.Settings

    let private logger = LogFactory.create "HttpServer"
 
    let host = sprintf "http://localhost:%d/" (!globalSettings).api.port

    let private running = ref false

    let isRunning () = !running

    let private listener (handler:(HttpListenerRequest -> HttpListenerResponse -> Async<unit>)) =
        let hl = new HttpListener()
        hl.Prefixes.Add host
        hl.Start()
        let task = Async.FromBeginEnd(hl.BeginGetContext, hl.EndGetContext)
        async {
            while !running do
                let! context = task
                Async.Start(handler context.Request context.Response)
        } |> Async.Start
 
    let private (|BeginsWith|_|) wut (s:string) = if s.StartsWith(wut) then Some () else None
        
    let private q = Event<_>()

    let onApiQuote = q.Publish

    let start () = 
        if not !running then
            running := true
            listener (fun req resp -> async {
                if not !running then return ()

                logger.TraceF "Got request with path %s" req.Url.AbsolutePath

                let answer = 
                    if req.HttpMethod = "POST" then
                        match req.Url.AbsolutePath with
                        | BeginsWith "/quote" ->
                            try
                                use ms = new MemoryStream()
                                req.InputStream.CopyTo(ms)                                
                                let quotes = ApiQuotes.unpack <| ms.ToArray()
                                logger.TraceF "Triggering with %A" quotes
                                q.Trigger quotes
                                "OK"
                            with :? JsonException as e ->   
                                logger.ErrorEx "Failed to parse" e
                                "ERR1 (Request parse error)"
                        | _ -> "ERR2 (Invalid service)"
                    else "ERR3 (Invalid request method)"

                let txt = Encoding.ASCII.GetBytes(answer)

                resp.ContentType <- "text/html"
                resp.OutputStream.Write(txt, 0, txt.Length)
                resp.OutputStream.Close()
            })

    let stop () = running := false