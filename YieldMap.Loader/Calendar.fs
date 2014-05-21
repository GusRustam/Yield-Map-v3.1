namespace YieldMap.Loader.Calendar

[<AutoOpen>]
module Calendar = 
    open System

    open YieldMap.Tools.Logging

    let private logger = LogFactory.create "Calendar"

    type Calendar = 
        abstract member Now : DateTime
        abstract member Today : DateTime
        abstract member NewDay : DateTime IEvent

    module private DateChangeTrigger = 
        let waitForTomorrow (evt:DateTime Event) (c : Calendar) = 
            let rec wait (time : DateTime) = async {
                do! Async.Sleep(1000) // midnight check once a second
                let now = c.Now
                if now.Date <> time.Date then
                    try
                        logger.TraceF "Triggering tomorrow"
                        evt.Trigger c.Today // tomorrow has come, informing
                    with e -> logger.ErrorF "Failed to handle date change: %s" (e.ToString())
                    return! wait now // and now we'll count from new time, current's today
                else return! wait time // counting from recent today
            }
            wait c.Now

    type RealCalendar() as this = 
        let dateChanged = Event<DateTime>()
        do DateChangeTrigger.waitForTomorrow dateChanged this |> Async.Start
        
        interface Calendar with
            member x.NewDay = dateChanged.Publish
            member x.Now = DateTime.Now
            member x.Today = DateTime.Today

    type MockCalendar(dt : DateTime) as this = 
        let _begin = DateTime.Now
        let dateChanged = Event<DateTime>()

        do DateChangeTrigger.waitForTomorrow dateChanged this |> Async.Start
        
        interface Calendar with
            member x.NewDay = dateChanged.Publish
            member x.Now = dt + (DateTime.Now - _begin)
            member x.Today = (x :> Calendar).Now.Date

    open System.Threading
    open YieldMap.Tools.Aux

    type UpdateableCalendar(dt : DateTime) as this = 
        let dateChanged = Event<DateTime>()
        let locker = obj ()

        let mutable _begin = DateTime.Now
        let mutable _pivot = dt
        let mutable token = new CancellationTokenSource()

        let startWaitTom () = 
            DateChangeTrigger.waitForTomorrow dateChanged this 
            |> Async.WithCancelToken token.Token 
            |> Async.Start

        do startWaitTom ()
        
        interface Calendar with
            member x.NewDay = dateChanged.Publish
            member x.Now = _pivot + (DateTime.Now - _begin)
            member x.Today = (x :> Calendar).Now.Date

        interface IDisposable with
            member x.Dispose () = token.Dispose ()

        member x.SetTime dt =
            lock locker (fun () -> 
                token.Cancel ()
                token.Dispose ()
                _pivot <- dt
                _begin <- DateTime.Now
                token <- new CancellationTokenSource ()
                startWaitTom ()
            )