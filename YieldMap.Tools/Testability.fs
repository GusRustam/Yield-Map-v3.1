namespace YieldMap.Tools

open Autofac
open Autofac.Builder
open Foq

#nowarn "62"
module Testability = 
    module Foq =
        let setup f (mock : 'T Mock) = mock.Setup f
        let returnsVal (item : 'b) (setup : ('a, 'b) ResultBuilder) = setup.Returns(item)
        let returnsCall (item : unit -> 'b) (setup : ('a, 'b) ResultBuilder) = setup.Returns(item)
        let create (mock : 'T Mock) = mock.Create()

//    module Autofac =
//        // todo create a computational expression for that
//        let create () = 
//            ContainerBuilder ()
//        let register<'T> (cb : ContainerBuilder) = 
//            cb.RegisterType<'T> (), cb
//        let instance<'T> instance (cb : ContainerBuilder) = 
//            cb.RegisterInstance instance, cb
//        let using<'T>  (b : IRegistrationBuilder<'T, SimpleActivatorData, SingleRegistrationStyle>) (cb : ContainerBuilder)  = 
//            b.As<'T> (), cb
//        let externallyOwned  (b : IRegistrationBuilder<'T, SimpleActivatorData, SingleRegistrationStyle>) (cb : ContainerBuilder) = 
//            b.ExternallyOwned (), cb
//        let build _ (cb : ContainerBuilder) = 
//            cb.Build ()
//
//        // todo this workflow is much more complex

    //    let private builder = ContainerBuilder ()
    //    let mutable private container = null;
    //    builder.RegisterType<ParsedGrammar>().As<Grammar>() |> ignore
    //    builder.RegisterType<InMemoryRegistry>().As<Registry>().SingleInstance() |> ignore
    //    let private factory = Func<IContainer>(fun () -> container)
    //    builder.RegisterInstance factory |> ignore
    //    container <- builder.Build ()

    //    let fakeDbSet = 
    //        Mock<DbSet<Property>>()
    //        |> Foq.setup (fun x -> <@ x.ToList() @>)
    //        |> Foq.returnsVal propertyList
    //        |> Foq.create
    //
    //    let fakeContext = 
    //        Mock<MainEntities>()
    //        |> Foq.setup (fun x -> <@ x.Properties @>)
    //        |> Foq.returnsVal fakeDbSet
    //        |> Foq.create
    //
    //    let fakeDbConn = 
    //        Mock<IDbConn>()
    //        |> Foq.setup (fun x -> <@ x.CreateContext() @>)
    //        |> Foq.returnsVal null
    //        |> Foq.create
    //
    //    let container = 
    //        Autofac.create ()
    //        |> Autofac.instance fakeDbConn
    //        ||> Autofac.externallyOwned
    //        ||> Autofac.build

