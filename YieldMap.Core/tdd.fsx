#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Core\bin\debug\YieldMap.Core.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\packages\Autofac.3.5.0\lib\net40\Autofac.dll"
#endif

open Autofac
open Autofac.Builder

module SomeBuilder =
    module Autofac =
        // todo create a computational expression for that
        let create () = 
            ContainerBuilder ()

        let instance<'T> instance (cb : ContainerBuilder) = 
            cb.RegisterInstance instance, cb

        let register<'T> (cb : ContainerBuilder) = 
            cb.RegisterType<'T> (), cb

        let using<'T, 'U>  (b : IRegistrationBuilder<'T, IConcreteActivatorData, SingleRegistrationStyle>) (cb : ContainerBuilder)  = 
            b.As<'U> (), cb

        let externallyOwned  (b : IRegistrationBuilder<'T,  IConcreteActivatorData, SingleRegistrationStyle>) (cb : ContainerBuilder) = 
            b.ExternallyOwned (), cb

        let build _ (cb : ContainerBuilder) = 
            cb.Build ()

    //    let container = 
    //        Autofac.create ()
    //        |> Autofac.instance fakeDbConn
    //        ||> Autofac.externallyOwned
    //        ||> Autofac.build

    //    let private builder = ContainerBuilder ()
    //    let mutable private container = null
    //    builder.RegisterType<ParsedGrammar>().As<Grammar>() |> ignore
    //    builder.RegisterType<InMemoryRegistry>().As<Registry>().SingleInstance() |> ignore
    //    let private factory = Func<IContainer>(fun () -> container)
    //    builder.RegisterInstance factory |> ignore
    //    container <- builder.Build ()

    type IA = abstract Aa : unit
    type A = interface IA with member x.Aa = ()
    
    let cb = Autofac.create ()
    let mutable c = null
    let _, cb = Autofac.instance (obj()) cb

    cb.RegisterType<IA>().As<A>() |> ignore
    let r, cb = Autofac.register<IA> cb
    let r, cb = Autofac.using<IA, A> r cb
    

    
    type 'T Builder = 
    | Init 
    | Begin of (ContainerBuilder -> IRegistrationBuilder<'T, SimpleActivatorData, SingleRegistrationStyle>)
    | Progrss of (IRegistrationBuilder<'T, SimpleActivatorData, SingleRegistrationStyle> -> IRegistrationBuilder<'T, SimpleActivatorData, SingleRegistrationStyle>)
    | Finish of (ContainerBuilder -> IContainer)


    module Piping =
        let c1 = 
            let x = 1
            let y = 2
            x + y

        let c2 = 
            let x = 1 in
                let y = 2 in
                    x + y

        let c3 = 
            1 |> (fun x ->
                2 |> (fun y ->
                    x + y))

        let c3' = 
            1 |> (fun x ->
            2 |> (fun y ->
            x + y))

        let c4 = 
            let pipe x f = f x
            let return' x = x
            pipe 1 (fun x ->
                pipe 2 (fun y -> 
                    return' x + y))

        let c5 = 
            let pipe x f = f x
            let return' x = x
            pipe 1 (fun x ->
            pipe 2 (fun y -> 
            return' x + y))

        type PB() = 
            member __.Bind (x, f) = f x
            member __.Return x = x

        let pb = PB()

        let c6 = pb {
            let! x = 1
            let! y = 2
            return x + y
        }