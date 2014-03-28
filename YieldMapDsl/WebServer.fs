namespace YieldMap.WebServer

module HttpServer = 
    open Newtonsoft.Json

    open System
    open System.Net
    open System.Text
    open System.IO

    open YieldMap.Logging
    open YieldMap.Settings

    let private logger = LogFactory.create "HttpServer"
 
    let host = sprintf "http://localhost:%d/" (!globalSettings).api.port

    let private running = ref false

    type ApiQuote() =
        member val Ric = String.Empty with get, set
        member val Field = String.Empty with get, set
        member val Value = String.Empty with get, set

    type ApiQuotes() = member val Quotes : ApiQuote array = Array.empty with get, set

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
        running := true
        listener (fun req resp -> async {
            logger.Info <| sprintf "Got request with params %s | %s | %s | %A" req.RawUrl req.Url.PathAndQuery req.Url.AbsolutePath req.QueryString.AllKeys

            let answer = //"HELLO"
                if req.HttpMethod = "POST" then
                    match req.Url.AbsolutePath with
                    | BeginsWith "/quote" ->
                        use reader = new StreamReader(req.InputStream, req.ContentEncoding)
                        let query = reader.ReadToEnd()
                        try
                            let quotes = JsonConvert.DeserializeObject<ApiQuotes>(query)
                            logger.Debug <| sprintf "Triggering with %A" quotes
                            q.Trigger quotes
                            "OK"
                        with :? JsonException as e ->   
                            logger.ErrorEx "Failed to parse" e
                            "ERR1"
                    | _ -> "ERR2"
                else "ERR3"

            let txt = Encoding.ASCII.GetBytes(answer)


            resp.ContentType <- "text/html"
            resp.OutputStream.Write(txt, 0, txt.Length)
            resp.OutputStream.Close()
        })

    let stop () = running := false

//    type HttpServer2 = 
//            static member private running = ref false
//
//            static member private q = Event<_>()
//
//            static member private listener (handler:(HttpListenerRequest->HttpListenerResponse->Async<unit>)) =
//                let hl = new HttpListener()
//                hl.Prefixes.Add HttpServer2.host
//                hl.Start()
//                let task = Async.FromBeginEnd(hl.BeginGetContext, hl.EndGetContext)
//                async {
//                    while !(HttpServer2.running) do
//                        let! context = task
//                        Async.Start(handler context.Request context.Response)
//                } |> Async.Start
//
//            static member host with get() = sprintf "http://localhost:%d/" (!globalSettings).api.port
//
//            static member quotes = HttpServer2.q.Publish
// 
//            static member start () = 
//                if not !(HttpServer2.running) then
//                    logger.Info "Starting"
//                    HttpServer2.running := true
//                    HttpServer2.listener (fun req resp -> async {
//                        logger.Info <| sprintf "Got request with params %s | %s | %s | %A" req.RawUrl req.Url.PathAndQuery req.Url.AbsolutePath req.QueryString.AllKeys
//
//                        let answer = "HELLO"
//    //                        if req.HttpMethod = "POST" then
//    //                            match req.Url.AbsolutePath with
//    //                            | BeginsWith "/quote" ->
//    //                                use reader = new StreamReader(req.InputStream, req.ContentEncoding)
//    //                                let query = reader.ReadToEnd()
//    //                                try
//    //                                    let quotes = JsonConvert.DeserializeObject<ApiQuotes>(query)
//    //                                    logger.Debug <| sprintf "Triggering with %A" quotes
//    //                                    HttpServer.q.Trigger quotes
//    //                                    "OK"
//    //                                with :? JsonException as e ->   
//    //                                    logger.ErrorEx "Failed to parse" e
//    //                                    "ERR"
//    //                            | _ -> "ERR"
//    //                        else "ERR"
//
//                        let txt = Encoding.ASCII.GetBytes(answer)
//                        resp.ContentType <- "text/plain"
//                        resp.OutputStream.Write(txt, 0, txt.Length)
//                        resp.OutputStream.Close()
//                    })
//
//            static member stop () = HttpServer2.running := false
//
//module WcfServer = 
//    open System
//    open System.Collections.Generic
//    open System.ServiceModel
//    open System.ServiceModel.Description
//    open System.ServiceModel.Web
//    open System.Text
//
//    open YieldMap.Logging
//
//    let private logger = LogFactory.create "WcfServer"
// 
//    [<ServiceContract>]
//    type Service =
//        [<OperationContract; WebGet>]
//        abstract EchoWithGet : s:string -> string
//
//        [<OperationContract; WebInvoke>]
//        abstract EchoWithPost : s:string -> string
//
//    type TheService = 
//        interface Service with
//            member x.EchoWithGet s = "You got: " + s
//            member x.EchoWithPost s = "You posted: " + s
//
//
//    let host = ref null
//
//    let start () =
//        host := new WebServiceHost(typeof<TheService>, Uri "http://localhost:8090")
//        let h = !host
//
//        try
//            let p = h.AddServiceEndpoint(typedefof<Service>, WebHttpBinding(), "")
//            h.Open ()
//            
//        with :? CommunicationException as e -> 
//            logger.WarnEx "Failure" e
//            h.Abort ()
//            host := null
//
//    let stop () =
//        let h = !host
//        try h.Close ()
//        with :? CommunicationException as e -> 
//            logger.WarnEx "Failure" e
//            h.Abort ()
//        
//        host := null
//        