namespace YieldMap.Core

module Recalculator =
    open Autofac

    open YieldMap.Core.DbManager
    open YieldMap.Tools.Response
    open YieldMap.Transitive.Registry
    open YieldMap.Transitive.Repositories

    open System.Linq
    
    let recalculate (db:DbManager) = async {
        try
            let container = db.dbContainer

            let registry = container.Resolve<IFunctionRegistry>()
            let updater = container.Resolve<IPropertiesUpdater>()
            let properyReader = container.Resolve<IPropertiesRepository> ()

            properyReader
                .FindAll()
                .ToList()
                |> Seq.iter (fun x -> registry.Add(x.id, x.Expression) |> ignore)

            updater.RecalculateBonds () |> ignore

            return Tweet.Answer ()
        with e -> return Tweet.Failure <| Failure.Error e
    }

module Loader = 
    open DbManager

    open YieldMap.Database

    open YieldMap.Core
    open YieldMap.Core.Notifier

    open YieldMap.Loader.MetaChains
        
    open YieldMap.Requests
    open YieldMap.Requests.MetaTables

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Aux.Workflows.Attempt
    open YieldMap.Tools.Response
    open YieldMap.Tools.Location
    open YieldMap.Tools.Logging

    open YieldMap.Transitive.MediatorTypes
    open YieldMap.Transitive.Queries

    open System
    open System.Collections.Generic
    open System.IO

    let private logger = LogFactory.create "Load"

    let loadChains (m:ChainMetaLoader) chains = async {
        let names = chains |> Array.map (fun r -> r.Ric)
        let! results = 
            chains 
            |> Seq.map (fun request -> m.LoadChain request |> Async.Map (fun res -> res, request))
            |> Async.Parallel
            
        let results = results |> Array.zip names

        let rics = results |> Array.map (fun (chainRic, res) -> 
            match res with 
            | (Answer rics, req) -> (chainRic, rics, req) 
            | (_, req) -> (chainRic, [||], req))

        let fails = results |> Array.choose (fun (chainRic, res) -> 
            match res with 
            | (Failure e, req) -> Some (chainRic, e, req) 
            | _ -> None)
                    
        do fails |> Array.iter (fun (ric, e, _) -> logger.WarnF "Failed to load chain %s because of %s" ric (e.ToString()))
            
        return rics, fails
    }

    let (|Floater|Convertible|Straight|) (b:BondDescr) = 
        if b.IsFloater then Floater b
        elif b.IsConvertible then Convertible b
        else Straight b

    let mutable backupFile = ""

    let loadAndSaveMetadata (s:Drivers) rics =
        tweet {
            let loader = s.Loader
            let db = s.Database
            
            let! bonds = loader.LoadMetadata<BondDescr> rics
            let! frns = loader.LoadMetadata<FrnData> rics

            let frnMap = frns |> List.map (fun x -> x.Ric, x) |> Map.ofList

            let toSave = bonds |> List.choose (function
                | Floater note when frnMap |> Map.containsKey note.Ric -> 
                    Frn.Create (frnMap.[note.Ric], note) 
                    :> InstrumentDescription 
                    |> Some
                | Floater note  -> 
                    logger.WarnF "No frn info on frn %s" note.Ric 
                    None
                | Straight bond -> 
                    Bond.Create bond
                    :> InstrumentDescription 
                    |> Some
                | Convertible conv -> 
                    logger.WarnF "Convertibles not supported yet %s" conv.Ric 
                    None)

            db |> DbManager.saveBonds (seq toSave)

            let! issueRatings = loader.LoadMetadata<IssueRatingData> rics
            let! issuerRatings = loader.LoadMetadata<IssuerRatingData> rics

            let iR = (issueRatings |> List.map Rating.Create) @ (issuerRatings |> List.map Rating.Create)
            db |> DbManager.saveRatings iR
                            
        }

    let rec reload (s:Drivers) chains force  = 
        let needsReload = s.Database |> DbManager.needsRefresh s.TodayFix

        async {
            if force || force && needsReload then
                try
                    backupFile <- DbManager.backup s.Database
                    return! load s chains
                with e -> 
                    logger.ErrorEx "Load failed" e
                    return! loadFailed s (Error e)
            else return Answer ()
        }

    and private load (s:Drivers) requests = 
        logger.Trace "load ()"
        async {
            try
                let! ricsByChain, fails = loadChains s.Loader requests

                // reporting errors
                fails |> Array.iter (fun (ric, e, _) -> 
                    Notifier.notify ("Loading", Problem <| sprintf "Failed to load chain %s because of %s" ric (e.ToString()), Severity.Warn))
                    

                // saving rics and chains
                ricsByChain |> Array.iter (fun (chain, rics, req) -> DbManager.saveChainRics s.Database chain rics req.Feed s.TodayFix req.Mode)

                // extracting rics
                let chainRics = ricsByChain |> Array.map snd3 |> Array.collect id |> set
                    
                // now determine which rics to reload and refresh
                let classified = DbManager.classify s.Database s.TodayFix (chainRics |> Set.toArray) 

                logger.InfoF "Will reload %d, kill %d and keep %d rics" 
                    (classified.[Mission.ToReload].Length) 
                    (classified.[Mission.Obsolete].Length) 
                    (classified.[Mission.Keep].Length)

// TODO PROPER DELETION USING PROPER RIC COMPARISON
//                let o = classified.[Mission.Obsolete]
//                let f = fun (r:Ric) -> Array.exists ((=) r.Name) o
//                s.Database |> DbManager.deleteRics (Func<Ric, bool>(f))
                    
                let! res = loadAndSaveMetadata s classified.[Mission.ToReload]
                match res with 
                | Some e -> return! loadFailed s e
                | None -> return Answer ()
            with e -> 
                logger.ErrorEx "Load failed" e
                return! loadFailed s (Error e)
        }

    and private loadFailed (s:Drivers) (e:Failure) = 
        logger.Trace "loadFailed ()"
        async {
            try 
                s.Database |> DbManager.restore backupFile
                logger.ErrorF "Failed to reload data, restored successfully: %A" (e.ToString())
                return Ping.Failure (Problem "Failed to reload data, restored successfully")
            with e ->
                logger.ErrorEx "Failed to reload and restore data" e
                return Ping.Failure (Problem "Failed to reload and restore data")
        }

    let estimate () = 5*60*1000 // 5 minutes is a great estimation m'lord