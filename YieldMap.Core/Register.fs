namespace YieldMap.Core

open Autofac

open YieldMap.Database.Access

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

    type Grammar =
        abstract Expression : string
        abstract Evaluate : (string, obj) Map -> Lexan.Value

    type ParsedGrammar (expr) = 
        let syntax = Syntan.grammarize expr
        interface Grammar with
            member x.Expression = expr
            member x.Evaluate env = Interpreter.evaluate syntax env

    type Registry = 
        abstract Clear : unit -> unit
        abstract Refresh : (int64 * string) seq -> unit
        abstract Items : unit -> (int64, Grammar) Map
        abstract Evaluate : int64 -> (string, obj) Map -> Lexan.Value

    // todo create a couple of swaps and a couple of other instruments so that to test on other types of instruments

    // свопы можно повесить на один и тот же айсин. тогда, кстати, надо удостовериваться, что 
    // нету дубляжа на уровне айсинов (тест с арменией)
    // то есть своп - это чейн плюс стуктура. плюс правило для выделения сроков. плюс ноги
    // так что же - опять xml? Или просто особый порядок загрузки чейнов?

    // todo recalculate functions!
    // 1) for each instrument get its id and a corresponding expression (from Property table)
    // 2) for each one extract metadata, pack it into a map, and evaluate expression, and store that value into the property table

    type InMemoryRegistry (factory : Func<IContainer>) =
        let container = factory.Invoke ()
        static let registry = ConcurrentDictionary<int64, Grammar> ()

        interface Registry with
            member x.Clear () = registry.Clear()
            member x.Refresh properties =
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

    let private builder = ContainerBuilder ()
    let mutable private container = null;
    builder.RegisterType<ParsedGrammar>().As<Grammar>() |> ignore
    builder.RegisterType<InMemoryRegistry>().As<Registry>().SingleInstance() |> ignore
    let private factory = Func<IContainer>(fun () -> container)
    builder.RegisterInstance factory |> ignore
    container <- builder.Build ()

    let defaultRegistry = container.Resolve<Registry> ()