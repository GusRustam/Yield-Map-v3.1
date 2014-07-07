namespace YieldMap.Tests.Unit

open Autofac
open FsUnit
open NUnit.Framework
open Rhino.Mocks
open System
open System.Linq

open YieldMap.Database
open YieldMap.Transitive
open YieldMap.Transitive.Domains.Repositories

module Database =
    let mocker () = 
        let feed = Feed(Name = "Q", Description = "")

        let mock = MockRepository.GenerateMock<IFeedRepository>()
        RhinoMocksExtensions.Stub<_,_>(mock, Rhino.Mocks.Function<_,_>(fun x -> x.FindAll()))
            .Return(([feed] |> Seq.ofList).AsQueryable()) 
            |> ignore
        mock

    [<Test>]
    let ``Reading mock database`` () = 
        let builder = ContainerBuilder()

        let feedRepo = mocker()
        builder.RegisterInstance(feedRepo) |> ignore
        let container = builder.Build()

        use feeds = container.Resolve<IFeedRepository>()
        feeds.FindAll().Count() |> should be (equal 1)

    [<Test>]
    let ``Reading real database`` () = 
        let builder = ContainerBuilder()

        let feedRepo = mocker()
        builder.RegisterType<FeedRepository>().As<IFeedRepository>() |> ignore
        let container = builder.Build()

        use feeds = container.Resolve<IFeedRepository>()
        feeds.FindAll().Count() |> should be (equal 1)
        
        
//        use uow = container.Resolve<IChainRicUnitOfWork>()
//        use repo = container.Resolve<IChainRepository>(NamedParameter("uow", uow))
//
//        let chain = Chain()
//        chain.Expanded <- Nullable DateTime.Today
//        chain.Feed

//        repo.Add <| Chain()