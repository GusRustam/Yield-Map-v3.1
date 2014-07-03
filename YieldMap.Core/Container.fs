namespace YieldMap.Core

open Autofac

open YieldMap.Language

open System

open Register

module Container = 
//    type SomeBuilder () =
//        let builder = ContainerBuilder ()
//        let mutable container = null
//
//        member __.Bind (x, f) = f x
//
//        member __.Yield(()) = 
//            builder.RegisterInstance (Func<IContainer>(fun () -> container)) |> ignore
//            builder.Build ()
//
//        member __.Zero () =
//            builder.RegisterInstance (Func<IContainer>(fun () -> container)) |> ignore
//            builder.Build ()
//
//        [<CustomOperation("xxx")>]
//        member __.Xxx (x) = ()
//
//        [<CustomOperation("registerAs", MaintainsVariableSpace = true)>]
//        member __.RegisterAs<'T, 'U> (x) = builder.RegisterType<'T>().As<'U>() |> ignore
//
//    let builder1 = SomeBuilder ()
//
//    let z = builder1 {
//        xxx
//    }

//    type BuildExpression = BuildExpression of (ContainerBuilder ->  ContainerBuilder)
//
//    type SomeBuilder () =
//        let builder = ContainerBuilder ()
//        let mutable container = null
//        let finish () = 
//            builder.RegisterInstance (Func<IContainer>(fun () -> container)) |> ignore
//            builder
//
//        member __.Bind (x, f) = f builder
//        member __.Zero () = finish ()
//
//    let builder1 = SomeBuilder ()
//
//    let registerAs<'T, 'U> = BuildExpression (fun (cb:ContainerBuilder) -> 
//                                                cb.RegisterType<'T>().As<'U>() |> ignore
//                                                cb) 
//
//    let b = builder1 {
//        ()
//    }
//
//    let a = builder1 {
//        let y = ()
//        let x = registerAs<ParsedGrammar, Grammar>
//
//        ()
//    }

    let private builder = ContainerBuilder ()
    
    let mutable container = null;
    builder.RegisterType<DbRecounter>().As<Recounter>() |> ignore
    builder.RegisterType<ParsedGrammar>().As<Grammar>() |> ignore
    builder.RegisterType<InMemoryRegistry>().As<Registry>().SingleInstance() |> ignore
    builder.RegisterInstance (Func<IContainer>(fun () -> container)) |> ignore

    container <- builder.Build ()