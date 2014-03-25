namespace YieldMap.Calendar

[<AutoOpen>]
module Calendar = 
    open System

    open YieldMap.Logging

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
                        logger.Trace "Triggering tomorrow"
                        evt.Trigger c.Today // tomorrow has come, informing
                    with e -> logger.Error <| sprintf "Failed to handle date change: %s" (e.ToString())
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

    let defaultCalendar = RealCalendar() :> Calendar