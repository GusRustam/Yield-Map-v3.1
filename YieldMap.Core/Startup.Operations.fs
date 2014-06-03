namespace YieldMap.Core.Application.Operations

[<AutoOpen>]
module Operations =
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.LiveQuotes
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Logging

    open YieldMap.Requests
    open YieldMap.Requests.Responses

    open YieldMap.Transitive

    open System

    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
    }

    type ('Request, 'Answer) Operation =
        abstract Estimate : unit -> int
        abstract Execute : 'Request * int option -> 'Answer Tweet Async

    module Operation =
        let estimate (o : Operation<_,_>) = function Some t -> t | _ -> o.Estimate ()
        let execute (o : Operation<unit,_>) t = o.Execute ((), Some t)

    type LoadAndSaveRequest = {
        Chains : ChainRequest array
        Force : bool
    }

    let private logger = LogFactory.create "Operations"

    module Loading = 
        open YieldMap.Core.Notifier

        open YieldMap.Database
        open YieldMap.Database.StoredProcedures

        open YieldMap.Loader.MetaChains
        
        open YieldMap.Requests.Responses
        open YieldMap.Requests.MetaTables

        open YieldMap.Tools.Aux
        open YieldMap.Tools.Aux.Workflows.Attempt
        open YieldMap.Tools.Location
        open YieldMap.Tools.Logging

        open System
        open System.Collections.Generic
        open System.IO

        let private logger = LogFactory.create "Loading"

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

        let loadAndSaveMetadata (s:Drivers) rics =
            tweet {
                let loader = s.Loader
                use iBonds = new Additions.Bonds ()
            
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

                iBonds.Save toSave

                let iRatings = new Additions.Ratings ()
                let! issueRatings = loader.LoadMetadata<IssueRatingData> rics
                let! issuerRatings = loader.LoadMetadata<IssuerRatingData> rics

                let iR = (issueRatings |> List.map Rating.Create) @ (issuerRatings |> List.map Rating.Create)
                iRatings.SaveRatings iR
                            
            }

        let rec reload (s:Drivers) chains force  = 
            let loader, dt = s.Loader, s.TodayFix

            let refresh = new Refresh ()
            let needsReload = refresh.NeedsReload s.TodayFix

            async {
                if force || force && needsReload then
                    try
                        BackupRestore.Backup ()
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
                    
                    use db = new Additions.ChainRics ()

                    // saving rics and chains
                    ricsByChain |> Array.iter (fun (chain, rics, req) -> db.SaveChainRics(chain, rics, req.Feed, s.TodayFix, req.Mode))

                    // extracting rics
                    let chainRics = ricsByChain |> Array.map snd3 |> Array.collect id |> set
                    
                    // now determine which rics to reload and refresh
                    let classified = ChainsLogic.Classify (s.TodayFix, chainRics |> Set.toArray)

                    logger.InfoF "Will reload %d, kill %d and keep %d rics" 
                        (classified.[Mission.ToReload].Length) 
                        (classified.[Mission.Obsolete].Length) 
                        (classified.[Mission.Keep].Length)

                    // todo delete obsolete rics <- definitely a stored procedure 
                    // todo should I do a cleanup here?

                    // TODODODODO!
//                    try Additions.DeleteBonds <| HashSet<_>(classified.[Mission.Keep])
//                    with e -> logger.ErrorEx "Failed to cleanup" e
                    
                    let! res = loadAndSaveMetadata s classified.[Mission.ToReload]
                    match res with 
                    | Some e -> return! loadFailed s e
                    | None -> 
                        BackupRestore.Cleanup ()
                        return Answer ()
                with e -> 
                    logger.ErrorEx "Load failed" e
                    return! loadFailed s (Error e)
            }

        and private loadFailed (s:Drivers) (e:Failure) = 
            logger.Trace "loadFailed ()"
            async {
                try 
                    BackupRestore.Restore ()
                    logger.ErrorF "Failed to reload data, restored successfully: %A" (e.ToString())
                    return Ping.Failure (Problem "Failed to reload data, restored successfully")
                with e ->
                    logger.ErrorEx "Failed to reload and restore data" e
                    return Ping.Failure (Problem "Failed to reload and restore data")
            }

        let estimate () = 5*60*1000 // 5 minutes is a great estimation m'lord

    type LoadAndSave (s:Drivers) = 
        interface Operation<LoadAndSaveRequest, unit> with
            member x.Estimate () = Loading.estimate ()
            member x.Execute (r, ?t) = Loading.reload s r.Chains r.Force

    type EstablishConnection (f:EikonFactory) = 
        interface Operation<unit, unit> with
            member x.Estimate () = 15000 // fifteen seconds would suffice
            member x.Execute (_, ?timeout) = async { 
                let! res = 
                    match timeout with
                    | Some t -> f.Connect t
                    | _ -> f.Connect ()
                
                return 
                    match res with
                    | Ping.Failure f -> Tweet.Failure f
                    | Ok -> Answer ()
            }

    type Shutdown () = 
        interface Operation<unit, unit> with
            member x.Estimate () = 500 // half a second should be enough
            member x.Execute (_, _) = async { 
                do! Async.Sleep 500
                return Answer () 
            }