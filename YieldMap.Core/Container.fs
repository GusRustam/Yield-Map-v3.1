namespace YieldMap.Core

open Autofac

open YieldMap.Language

open System

module Container = 
    let private builder = ContainerBuilder ()
    
    let mutable container = null;
    builder.RegisterInstance (Func<IContainer>(fun () -> container)) |> ignore

    container <- builder.Build ()