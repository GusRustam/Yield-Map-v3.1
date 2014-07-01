namespace YieldMap.Core

open Autofac

open YieldMap.Database
open YieldMap.Database.Access
open YieldMap.Database.Procedures.Additions

open YieldMap.Language
open YieldMap.Language.Exceptions
open YieldMap.Tools.Aux
open YieldMap.Tools.Logging

open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open System

#nowarn "62"

module Register =
    let logger = LogFactory.create "Core.Register"

    /// Abstract grammar
    type Grammar =
        abstract Expression : string
        abstract Evaluate : (string, obj) Map -> Lexan.Value

    type ParsedGrammar (expr) = 
        let syntax = Syntan.grammarize expr
        interface Grammar with
            member x.Expression = expr
            member x.Evaluate env = Interpreter.evaluate syntax env

    /// A dictionary of id -> Grammar pairs
    /// The id is id_Property, grammar is result of parsing the expression in that property
    type Registry = 
        /// Remove all items from registry
        abstract Clear : unit -> unit

        /// Adds all given properties to the dictionary
        /// int64 is id_Property, string is expression
        abstract Add : (int64 * string) seq -> unit

        /// Returns cloned copy of all items
        abstract Items : unit -> (int64, Grammar) Map

        /// Evaluates specific item against given variables
        abstract Evaluate : int64 -> (string, obj) Map -> Lexan.Value

        /// Evaluates all items against given variables
        abstract EvaluateAll : (string, obj) Map -> (int64 * string) list

    type InMemoryRegistry (factory : Func<IContainer>) =
        let container = factory.Invoke ()
        static let registry = ConcurrentDictionary<int64, Grammar> ()

        interface Registry with
            member x.Clear () = registry.Clear()
            member x.Add properties =
                properties |> Seq.iter (fun (id, expr) -> 
                    registry.TryRemove id |> ignore
                    if not <| String.IsNullOrWhiteSpace expr then
                        try
                            let grammar = container.Resolve<Grammar>(NamedParameter("expr", expr))
                            if not <| registry.TryAdd (id, grammar) then
                                logger.Warn <| sprintf "Failed to add item with id %d into registry" id
                        with :? GrammarException as e -> 
                            logger.WarnEx (sprintf "Failed to interpret an expression %s" expr ) e
                )

            member x.Items () = 
                Dictionary<_,_> registry |> Map.fromDict

            member x.Evaluate id env = 
                let success, v = registry.TryGetValue id
                
                if success then
                    try
                        v.Evaluate env
                    with :? InterpreterException as e ->
                        logger.WarnEx (sprintf "Failed to interpret an expression %s with params %A" v.Expression env) e
                        Lexan.Nothing
                else Lexan.Nothing

            member x.EvaluateAll env = 
                registry.Keys |> List.ofSeq |> List.map (fun id -> id, ((x :> Registry).Evaluate id env).asString)

    let pack (i : InstrumentDescriptionView) = 
        [("Name", box i.InstrumentName)
         ("Borrower.Country", box i.BorrowerCountryName)
         ("Borrower.Name", box i.BorrowerName)
         ("Issuer.Country", box i.IssuerCountryName)
         ("Issuer.Name", box i.IssuerName)
         ("Industry", box i.IndustryName)
         ("Issue.Rating", box i.InstrumentRating)
         ("Issue.RatingAgency", box i.InstrumentRatingAgency)
         ("Issue.RatingDate", box i.InstrumentRatingDate)
         ("Issuer.Rating", box i.IssuerRating)
         ("Issuer.RatingAgency", box i.IssuerRatingAgency)
         ("Issuer.RatingDate", box i.IssuerRatingDate)
         ("Isin", box i.IsinName)
         ("Ric", box i.RicName)
         ("IssueDate", box i.Issue)
         ("Maturity", box i.Maturity)
         ("IssueSize", box i.IssueSize)
         ("Industry", box i.IndustryName)
         ("SubIndustry", box i.SubIndustryName)
         ("Seniority", box i.SeniorityName)
         ("Series", box i.Series)
         ("Specimen", box i.SpecimenName)
         ("Ticker", box i.TickerName)] |> Map.ofList

    type Recounter = abstract member Recount : unit -> unit
    type DbRecounter(cont : Func<IContainer>) = 
        let container = cont.Invoke ()
        let conn = container.Resolve<IDbConn> ()
        let registry = container.Resolve<Registry> ()
        let propertySaver = container.Resolve<IPropertyStorage> ()

        interface Recounter with
            member x.Recount () = 
                use ctx = conn.CreateContext ()


                ctx.Instruments 
                |> Seq.choose (fun i -> 
                    let descr = query { for d in ctx.InstrumentDescriptionViews do
                                        where (d.id_Instrument = i.id)                                         
                                        select (pack d) 
                                        exactlyOneOrDefault }                
                    if not <| Map.isEmpty descr 
                    then Some (i.id, registry.EvaluateAll descr |> Map.ofSeq)
                    else None)
                |> Map.ofSeq
                |> Map.toDict2 // (idInstrument (idProperty, value) Dict) Dict
                |> propertySaver.Save 

    
    // todo wtf's going on here?
    let private builder = ContainerBuilder ()
    let mutable private container = null;
    builder.RegisterType<ParsedGrammar>().As<Grammar>() |> ignore
    builder.RegisterType<InMemoryRegistry>().As<Registry>().SingleInstance() |> ignore
    let private factory = Func<IContainer>(fun () -> container)
    builder.RegisterInstance factory |> ignore
    container <- builder.Build ()

    let defaultRegistry = container.Resolve<Registry> () 
