namespace YieldMap.Tests.Unit

open Autofac
open FsUnit
open NUnit.Framework
open Rhino.Mocks
open System
open System.Linq

open YieldMap.Database
open YieldMap.Requests.MetaTables
open YieldMap.Transitive
open YieldMap.Transitive.Domains.UnitsOfWork
open YieldMap.Transitive.Procedures
open YieldMap.Transitive.Repositories
open YieldMap.Transitive.MediatorTypes

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

    [<Test>]
    let ``Add feed to real database, save and then remove it`` () =
        let container = DatabaseBuilder.Container

        use uow = container.Resolve<IEikonEntitiesUnitOfWork>()
        use feeds = container.Resolve<IFeedRepository>(NamedParameter("uow", uow))

        let feed = feeds.FindById 1L
        feed.Name |> should be (equal "Q")

        let feed = Feed(Name = "W", Description = "Test item")
        feeds.Add feed |> should be (equal 0)
        uow.Save () |> should be (equal 1)

        feed.id |> should be (greaterThan 0)
        let newId = feed.id

        use feeds2 = container.Resolve<IFeedRepository>()
        let feed2 = feeds2.FindById newId

        feed2.Name |> should be (equal "W")

        feeds.Remove feed |> should be (equal 0)
        uow.Save () |> should be (equal 1)

        use feeds3 = container.Resolve<IFeedRepository>()
        let feed3 = feeds3.FindById newId
        feed3 |> should be (equal null)


    [<Test>]
    let ``Add chain and ric to real database, save and then remove it`` () =
        let container = DatabaseBuilder.Container

        using (container.Resolve<IChainRepository>()) (fun chains ->
            chains.FindAll().Count() |> should be (equal 0))
        using (container.Resolve<IRicRepository>()) (fun rics ->
            rics.FindAll().Count() |> should be (equal 0))

        let chainRicSaver = container.Resolve<IChainRics> ()
        chainRicSaver.SaveChainRics("TESTCHAIN", [|"TESTRIC"|], "Q", DateTime.Today, "")

        let chainId = ref 0L
        let ricId = ref 0L
        using (container.Resolve<IChainRepository>()) (fun chains ->
            let all = chains.FindAll()
            all.Count() |> should be (equal 1)
            chainId := all.First().id)
        using (container.Resolve<IRicRepository>()) (fun rics ->
            let all = rics.FindAll()
            all.Count() |> should be (equal 1)
            ricId := all.First().id)

        using (container.Resolve<IChainRicUnitOfWork>()) (fun uow ->
        using (container.Resolve<IChainRepository>(NamedParameter("uow", uow))) (fun chains ->
        using (container.Resolve<IRicRepository>(NamedParameter("uow", uow))) (fun rics -> 
            let chain = chains.FindById !chainId
            chains.Remove chain |> should be (equal 0)
            let ric = rics.FindById !ricId
            rics.Remove ric |> should be (equal 0)
            uow.Save() |> should be (equal 2) )))

        using (container.Resolve<IChainRepository>()) (fun chains ->
            chains.FindAll().Count() |> should be (equal 0))
        using (container.Resolve<IRicRepository>()) (fun rics ->
            rics.FindAll().Count() |> should be (equal 0))

    [<Test>]
    let ``Create a bond, save it to real db, and then remove it`` () = 
        let container = DatabaseBuilder.Container

        let chainRicSaver = container.Resolve<IChainRics> ()
        chainRicSaver.SaveChainRics("TESTCHAIN", [|"TESTRIC"|], "Q", DateTime.Today, "")

        using (container.Resolve<IInstrumentRepository>()) (fun instruments ->
            instruments.FindAll().Count() |> should be (equal 0) |> ignore)

        let bondSaver = container.Resolve<IBonds>()
        
        let bond = MetaTables.BondDescr(BondStructure = "BondStructure", Description = "Description", Ric = "TESTRIC", Currency = "RUB")
                   |> Bond.Create 

        bondSaver.Save [bond]

        let id = ref 0L
        using (container.Resolve<IInstrumentRepository>()) (fun instruments ->
            let all = instruments.FindAll()
            all.Count() |> should be (equal 1) |> ignore
            id := all.First().id)

        using (container.Resolve<IBondAdditionUnitOfWork>()) (fun uow ->
        using (container.Resolve<IInstrumentRepository>(NamedParameter("uow", uow))) (fun instruments ->
            let bnd = instruments.FindById !id
            instruments.Remove bnd |> should be (equal 0)
            uow.Save () |> should be (equal 1) ))

        using (container.Resolve<IInstrumentRepository>()) (fun instruments ->
            instruments.FindAll().Count() |> should be (equal 0) |> ignore)

        using (container.Resolve<IChainRicUnitOfWork>()) (fun uow ->
        using (container.Resolve<IChainRepository>(NamedParameter("uow", uow))) (fun chains ->
        using (container.Resolve<IRicRepository>(NamedParameter("uow", uow))) (fun rics -> 
            let chain = chains.FindBy(fun c -> c.Name = "TESTCHAIN").ToList().First()
            chains.Remove chain |> should be (equal 0)
            let ric = rics.FindBy(fun r -> r.Name = "TESTRIC").ToList().First()
            rics.Remove ric |> should be (equal 0)
            uow.Save() |> should be (equal 2)
        )))