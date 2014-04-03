module Main 
    open System

    type IdName = {
        Id : int
        Name : string
    }

    type State = static member initialize () = () // some factory!

    type VM() =
        // todo а вот и структура классов
        member x.``portfolio -> open`` id = ()
        member x.``portfolio -> close`` id = ()

        member x.``bond-curve -> add`` id = ()
        member x.``bond-curve -> hide`` id = ()
        member x.``bond-curve -> bond -> add`` id isin = ()
        member x.``bond-curve -> bond -> hide`` id isin = ()
        member x.``bond-curve -> interpolate`` id mode = ()

        member x.``swap-curve -> add`` id = ()
        member x.``swap-curve -> hide`` id = ()
        member x.``swap-curve -> swap -> add`` id isin = ()
        member x.``swap-curve -> swap -> hide`` id isin = ()

        member x.``bond -> add`` isin portfolioId = ()
        member x.``bond -> hide`` isin = ()
        member x.``bond -> delete`` isin = ()

        member x.``custom-bond -> add`` id = ()
        member x.``custom-bond -> hide`` id = ()
        member x.``custom-bond -> delete`` id = ()

    type Ansamble() =
        class end

    type PM() = 
        static let portfolios = 
            [   
                {Id = 1; Name = "OFZ"}
                {Id = 2; Name = "Moscow"}
            ]

        static member ``portfolio -> exists`` id = 
            portfolios |> List.exists (fun idName -> idName.Id = id)

        static member ``portfolios -> list`` _ = portfolios

    type EM = 
        class end

    open EikonDesktopDataAPI
    
    open YieldMap.Axis.Program
    open YieldMap.Axis.Analytics
    open YieldMap.Axis.Ansamble
    
    open YieldMap.Loader.Loading

    open YieldMap.Tools

//    [<EntryPoint>]
    let main argv = 
//        let eikon = EikonDesktopDataAPIClass() :> EikonDesktopDataAPI 
//        let loader = OuterLoader(eikon) :> MetaLoader
//        
//        match loader.Connect() |> Async.RunSynchronously with
//        | Answers.Connected ->
//            // todo split loader
//
//            let main = {
//                Loader = loader
//                Factory = EikonFactory(eikon)
//                Time = CurrentTimeProvider()
//                QuoteQueue = null
//            }
//
//            let chart = { 
//                X = Macauley |> Duration |> Calc
//                Y = Yield |> Calc
//                Currency = Concrete "USD" 
//            }
//
//            let group = {
//                Instruments = []
//                Bootstrapped = Can't
//                Interpolation = LinearInterpolation
//            }
//
//            let party = {
//                Settings = { YieldMode = None }
//                Charts = [chart]
//                Calculators = []
//                Groups = [group]
//            }
//
//
//            // todo some call to get the party started
//
//            Console.WriteLine "Connected"
//        | _ -> Console.WriteLine "Failed to connect"
//        
//        Console.ReadKey() |> ignore
//        Ole32.killComObject <| ref<obj> eikon
//        Ole32.CoUnintialize()
        0