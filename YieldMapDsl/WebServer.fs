namespace YieldMap.WebServer

[<AutoOpen>]
module WebServer = 
    open Newtonsoft.Json
    open System
    open YieldMap.Tools

    type ApiQuote() =
        member val Ric = String.Empty with get, set
        member val Field = String.Empty with get, set
        member val Value = String.Empty with get, set
        override x.ToString() = sprintf "<%s | %s | %s>" x.Ric x.Field x.Value
        static member create ric field value = 
            let res = ApiQuote()
            res.Ric <- ric
            res.Field <- field
            res.Value <- value
            res

    type ApiQuotes() = 
        member val Quotes : ApiQuote array = Array.empty with get, set
        override x.ToString() = sprintf "%A" x.Quotes
        static member create arr = 
            let x = ApiQuotes()
            x.Quotes <- arr
            x
        static member pack (q : ApiQuotes) = 
            let ser = JsonConvert.SerializeObject(q)
            String.toBytes ser

        static member unpack b = 
            JsonConvert.DeserializeObject<ApiQuotes>(String.fromBytes b)

module ApiServer = 
    open Newtonsoft.Json

    open System
    open System.Net
    open System.Text
    open System.IO

    open YieldMap.Logging
    open YieldMap.Settings
    open YieldMap.Tools

    let private logger = LogFactory.create "HttpServer"
 
    let host = sprintf "http://localhost:%d/" (!globalSettings).api.port

    let private running = ref false


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

    let start () = 
        if not !running then
            running := true
            listener (fun req resp -> async {
                logger.Info <| sprintf "Got request with path %s" req.Url.AbsolutePath

                let answer = 
                    if req.HttpMethod = "POST" then
                        match req.Url.AbsolutePath with
                        | BeginsWith "/quote" ->
                            try
                                use ms = new MemoryStream()
                                req.InputStream.CopyTo(ms)                                
                                let quotes = ApiQuotes.unpack <| ms.ToArray()
                                logger.Debug <| sprintf "Triggering with %A" quotes
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