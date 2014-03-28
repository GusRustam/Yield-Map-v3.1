namespace YieldMap.WebServer

module HttpServer = 
    open System
    open System.Net
    open System.Text

    open YieldMap.Logging
    open YieldMap.Settings

    let private logger = LogFactory.create "HttpServer"
 
    let private host = sprintf "http://localhost:%d/" (!globalSettings).api.port

    let private running = ref false
 
    let private listener (handler:(HttpListenerRequest->HttpListenerResponse->Async<unit>)) =
        let hl = new HttpListener()
        hl.Prefixes.Add host
        hl.Start()
        let task = Async.FromBeginEnd(hl.BeginGetContext, hl.EndGetContext)
        async {
            while !running do
                let! context = task
                Async.Start(handler context.Request context.Response)
        } |> Async.Start
 
    let (|BeginsWith|_|) wut (s:string) = if s.StartsWith(wut) then Some () else None

    let start () = 
        running := true
        listener (fun req resp -> async {
            logger.Info <| sprintf "Got request with params %s | %s | %s | %A" req.RawUrl req.Url.PathAndQuery req.Url.AbsolutePath req.QueryString.AllKeys

            let answer = 
                if req.HttpMethod = "POST" then
                    match req.Url.AbsolutePath with
                    | BeginsWith "hi" -> ()
                    | _ -> ()
                    "OK"
                else
                    "ERR"


            let txt = Encoding.ASCII.GetBytes(answer)
            resp.ContentType <- "text/plain"
            resp.OutputStream.Write(txt, 0, txt.Length)
            resp.OutputStream.Close()
        })

    let stop () = running := false

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