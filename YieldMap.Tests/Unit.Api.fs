namespace YieldMap.Tests.Unit

open System

open YieldMap.Tests.Common

open NUnit.Framework
open FsUnit

module TestWebServer = 
    open System.Net
    open System.Text

    open YieldMap.Tools.Logging
    open YieldMap.Loader.WebServer
    open YieldMap.Tools.Aux

    let logger = LogFactory.create "TestWebServer"

    [<Test>]
    let ``Web server starts, responds and stops`` () = 
        ApiServer.start ()
        logger.InfoF "Server started"
        Async.Sleep(3000) |> Async.RunSynchronously

        use wb = new WebClient()
        let res = wb.DownloadString(ApiServer.host)
        logger.InfoF "Got answer %A" res
        res.Substring(0,3) |> should equal "ERR"

        ApiServer.stop ()
        logger.InfoF "Server stopping"
        Async.Sleep(3000) |> Async.RunSynchronously
        logger.InfoF "Server must be stopped"

        let req = wb.AsyncDownloadString <| Uri ApiServer.host
        try
            let q = Async.RunSynchronously(req, 5000)
            logger.ErrorF "got illegal answer %A" q
            let q = Async.RunSynchronously(req, 5000)
            logger.ErrorF "got illegal answer %A" q
            Assert.Fail()
        with :? TimeoutException ->
            logger.InfoF "Done"

    [<Test>]
    let ``Web server accepts quotes`` () = 
        ApiServer.start ()
        logger.InfoF "Seems 2B started"
        Async.Sleep(5000) |> Async.RunSynchronously

        logger.InfoF "Sending request"
        use wb = new WebClient()

        let q = ApiQuote.create "XXX" "FLD" "12"
        let z = ApiQuotes.create [|q|]
        let enc = ApiQuotes.pack z

        let resp = wb.UploadData(ApiServer.host + "quote", enc)
        let response = Encoding.ASCII.GetString(resp)

        logger.InfoF "Response is %s" response
        response |> should equal "OK"

        ApiServer.stop ()

module TestApiQuotes =
    open System.Net
    open System.Text
    open System.Threading

    open YieldMap.Loader.LiveQuotes
    open YieldMap.Tools.Aux
    open YieldMap.Loader.WebServer

    open YieldMap.Tools.Logging
    let logger = LogFactory.create "TestApiQuotes"

    [<Test>]
    let ``I recieve quotes I sent`` () = 
        let apiQuotes = ApiSubscription() :> Subscription

        let slots = [|
            ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "12"|]
            ApiQuotes.create [|ApiQuote.create "YYY" "ASK" "13"|]
            ApiQuotes.create [|ApiQuote.create "XXX" "BID" "95"|]
            ApiQuotes.create [|
                ApiQuote.create "YYY" "BID" "10"
                ApiQuote.create "YYY" "ASK" "15"
                ApiQuote.create "XXX" "ASK" "105"
            |]
        |]

        let count = ref 0

        let subscription = [
            ("YYY", ["BID"; "ASK"])
            ("XXX", ["BID"; "ASK"])
        ]

        subscription |> Map.ofList |> apiQuotes.Add 
        apiQuotes.Start()
        apiQuotes.OnQuotes 
        |> Observable.scan (fun (count, item) evt -> (count+1, evt)) (-1, Map.empty)
        |> Observable.add (fun (i, rfv) -> 
            count := i+1
            logger.InfoF "Got quotes %A" rfv
            try rfv |> should equal (ApiQuotes.toRfv slots.[i])
            with e -> logger.ErrorEx "Failed" e
            logger.InfoF "Ok"
        )

        use wb = new WebClient()

        slots |> Array.iter (fun slot -> 
            logger.InfoF "To send quotes %A" slot
            let enc = ApiQuotes.pack slot
            let resp = wb.UploadData(ApiServer.host + "quote", enc)
            let response = Encoding.ASCII.GetString(resp)
            response |> should equal "OK"
            Async.Sleep(1000) |> Async.RunSynchronously
        )

        !count |> should equal (Array.length slots)
        apiQuotes.Stop()

