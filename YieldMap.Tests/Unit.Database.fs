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
    let mockFeedRepo (feeds : Feed seq)= 
        let mock = MockRepository.GenerateMock<IFeedRepository>()
        RhinoMocksExtensions
            .Stub<_,_>(mock, Rhino.Mocks.Function<_,_>(fun x -> x.FindAll()))
            .Return(feeds.AsQueryable()) 
            |> ignore
        mock

    [<Test>]
    let ``Reading mock database`` () = 
        // Prepare
        let builder = ContainerBuilder()

        let feedRepo =
            [Feed(id = 1L, Name = "Q", Description = "")] 
            |> Seq.ofList
            |> mockFeedRepo
        
        builder.RegisterInstance(feedRepo) |> ignore
        let container = builder.Build()

        // Test
        use feeds = container.Resolve<IFeedRepository>()
        feeds.FindAll().Count() |> should be (equal 1)

    [<Test>]
    let ``Reading real database`` () = 
        // Prepare
        let builder = ContainerBuilder()
        builder.RegisterType<FeedRepository>().As<IFeedRepository>() |> ignore
        let container = builder.Build()

        // Test
        use feeds = container.Resolve<IFeedRepository>()
        feeds.FindAll().Count() |> should be (equal 1)

    [<Test>]
    let ``Adding chain to fake database`` () =
        // Prepare
        let builder = ContainerBuilder()

        let chainRepo = MockRepository.GenerateMock<IChainRepository>()

        RhinoMocksExtensions
            .Stub<_,_>(chainRepo, Rhino.Mocks.Function<_,_>(fun x -> x.Add(null)))
            .IgnoreArguments()
            .Return(1)
            |> ignore
        builder.RegisterInstance(chainRepo) |> ignore
        let container = builder.Build()

        // Test
        use chains = container.Resolve<IChainRepository>()

        // Varify
        chains.Add(Chain()) |> should be (equal 1)

        RhinoMocksExtensions.AssertWasCalled<_>(chainRepo, Func<IChainRepository,obj>(fun x -> x.Add(Rhino.Mocks.Arg<Chain>.Is.Anything) |> box))

    [<Test>]
    let ``Adding chain to fake database and saving it`` () =
        let builder = ContainerBuilder()

        let feed = Feed(id = 1L, Name = "Q", Description = "")
        let feedRepo = MockRepository.GenerateMock<IFeedRepository>()
        RhinoMocksExtensions
            .Stub<_,_>(feedRepo, Rhino.Mocks.Function<_,_>(fun x -> x.FindAll()))
            .Return(([feed]|> Seq.ofList).AsQueryable()) 
            |> ignore
        RhinoMocksExtensions
            .Stub<_,_>(feedRepo, Rhino.Mocks.Function<_,_>(fun x -> x.FindById(1L)))
            .Return(feed) 
            |> ignore            

        let chainRepo = MockRepository.GenerateMock<IChainRepository>()
        RhinoMocksExtensions
            .Stub<_,_>(chainRepo, Rhino.Mocks.Function<_,_>(fun x -> x.Add(null)))
            .IgnoreArguments()
            .Return(1)
            |> ignore

        let chainUow = MockRepository.GenerateMock<IChainRicUnitOfWork>()
        RhinoMocksExtensions
            .Stub<_,_>(chainUow, Rhino.Mocks.Function<_,_>(fun x -> x.Save()))
            .Return(1)
            |> ignore
        
        builder.RegisterInstance(chainUow) |> ignore
        builder.RegisterInstance(feedRepo) |> ignore
        builder.RegisterInstance(chainRepo) |> ignore
        let container = builder.Build()

        use chainSaver = container.Resolve<IChainRicUnitOfWork>()
        use chains = container.Resolve<IChainRepository>()
        use feeds = container.Resolve<IFeedRepository>()

        let feed = feeds.FindById 1L
        chains.Add(Chain(Name = "0#RUCORP=MM", Feed = feed)) |> should be (equal 1)
        chainSaver.Save() |> should be (equal 1)