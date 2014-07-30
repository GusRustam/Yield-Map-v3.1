namespace YieldMap.Tests.Unit

open Autofac
open Clutch.Diagnostics.EntityFramework
open FsUnit
open NUnit.Framework
open Rhino.Mocks

open System
open System.Linq

open YieldMap.Database
open YieldMap.Requests.MetaTables
open YieldMap.Transitive
open YieldMap.Transitive.Enums
open YieldMap.Transitive.Domains
open YieldMap.Transitive.Domains.Readers
open YieldMap.Transitive.Domains.UnitsOfWork
open YieldMap.Transitive.Procedures
open YieldMap.Transitive.Repositories
open YieldMap.Transitive.Registry
open YieldMap.Transitive.Tools
open YieldMap.Transitive.MediatorTypes
open YieldMap.Transitive.Events
open YieldMap.Tools.Aux
open YieldMap.Tools.Logging

module Database =
    open YieldMap.Transitive.Procedures
    open YieldMap.Transitive.Native.Crud
    open YieldMap.Transitive.Native.Entities

    let logger = LogFactory.create "UnitTests.Database"
    let str (z : TimeSpan Nullable) = 
        if z.HasValue then z.Value.ToString("mm\:ss\.fffffff")
        else "N/A"

    let mockFeedRepo (feeds : Feed seq)= 
        let mock = MockRepository.GenerateMock<IFeedRepository>()
        RhinoMocksExtensions
            .Stub<_,_>(mock, Rhino.Mocks.Function<_,_>(fun x -> x.FindAll()))
            .Return(feeds.AsQueryable()) 
            |> ignore
        mock

    let inline getCount<'U, ^T when ^T :> IDisposable 
                                 and ^T : (member FindAll : unit -> IQueryable<'U>)> () =
        let container = DatabaseBuilder.Container
        using (container.Resolve< ^T>()) (fun pvRepo ->
            let q = (^T : (member FindAll : unit -> IQueryable<'U>) pvRepo)
            q.Count())

    let inline checkExact<'U, ^T when ^T :> IDisposable 
                                 and ^T : (member FindAll : unit -> IQueryable<'U>)> num =
        getCount<'U, ^T> () |> should be (equal num)

    let inline checkZero<'U, ^T when ^T :> IDisposable 
                                 and ^T : (member FindAll : unit -> IQueryable<'U>)> () =
        checkExact<'U, ^T> 0


    let createChainRicInstrument (container:IContainer) chainName chainRics descrs  =
        // Setting up, adding chain and ric
        let saver = container.Resolve<ISaver> ()
        saver.SaveChainRics(chainName, chainRics, "Q", DateTime.Today, "")


        let bond = descrs
                   |> Seq.map Bond.Create 
                   |> Seq.map (fun x -> x :> InstrumentDescription)

        saver.SaveInstruments bond

        let ids = ref []

        using (container.Resolve<IInstrumentRepository>()) (fun repo -> 
            let instruments = repo.FindAll().ToList()
            ids := descrs 
                   |> List.map (fun descr -> instruments.FirstOrDefault(fun i -> i.Name = descr.ShortName))
                   |> List.map (fun i -> if i = null then -1L else i.id))

        !ids


    let createProperty name =
        let container = DatabaseBuilder.Container
        let thePv = ref null
        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertiesRepository>(NamedParameter("uow", uow))) (fun pvRepo ->
            let p = Property(Name = name)
            pvRepo.Add p |> should be (equal 0)
            uow.Save () |> should be (equal 1)
            thePv := pvRepo.FindBy(fun x -> x.Name = name).First()))
        thePv.contents.id

    let deleteProperty propertyId =
        let container = DatabaseBuilder.Container
        // Teardown, removing property
        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertiesRepository>(NamedParameter("uow", uow))) (fun pvRepo ->
            let p = pvRepo.FindById propertyId
            pvRepo.Remove p |> should be (equal 0)
            uow.Save () |> should be (equal 1)))

    let deleteChainRicInstrument () =
        let container = DatabaseBuilder.Container
        // Teardown, removing chain and ric
        using (container.Resolve<IChainRicUnitOfWork>()) (fun uow ->
        using (container.Resolve<IChainRepository>(NamedParameter("uow", uow))) (fun chains ->
        using (container.Resolve<IRicRepository>(NamedParameter("uow", uow))) (fun rics -> 
            let chain = chains.FindBy(fun c -> c.Name = "TESTCHAIN").ToList().First()
            chains.Remove chain |> should be (equal 0)
            let ric = rics.FindBy(fun r -> r.Name = "TESTRIC").ToList().First()
            rics.Remove ric |> should be (equal 0)
            uow.Save() |> should be (equal 2)
        )))

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
        {new ITriggerManager with
            member __.Handle (src, args) = logger.WarnF "Intercepted events on %s: %s!!!" (src.GetType().Name) (args.ToString())
            member __.get_Next () = null}
        |> Triggers.Initialize 
        
        let container = DatabaseBuilder.Container

        using (container.Resolve<IChainRepository>()) (fun chains ->
            chains.FindAll().Count() |> should be (equal 0))
        using (container.Resolve<IRicRepository>()) (fun rics ->
            rics.FindAll().Count() |> should be (equal 0))

        let saver = container.Resolve<ISaver> ()
        saver.SaveChainRics("TESTCHAIN", [|"TESTRIC"|], "Q", DateTime.Today, "")

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

        let saver = container.Resolve<ISaver> ()
        saver.SaveChainRics("TESTCHAIN", [|"TESTRIC"|], "Q", DateTime.Today, "")

        let cnt = getCount<Instrument, IInstrumentRepository>()

        
        let bond = MetaTables.BondDescr(BondStructure = "BondStructure", Description = "Description", Ric = "TESTRIC", Currency = "RUB")
                   |> Bond.Create 

        saver.SaveInstruments [bond]

        let id = ref 0L
        checkExact<Instrument, IInstrumentRepository> (cnt+1)

        using (container.Resolve<IInstrumentRepository>()) (fun instruments ->
            id := instruments.FindAll().First().id)

        using (container.Resolve<IInstrumentUnitOfWork>()) (fun uow ->
        using (container.Resolve<IInstrumentRepository>(NamedParameter("uow", uow))) (fun instruments ->
            let bnd = instruments.FindById !id
            instruments.Remove bnd |> should be (equal 0)
            uow.Save () |> should be (equal 1) ))

        checkExact<Instrument, IInstrumentRepository> cnt

        using (container.Resolve<IChainRicUnitOfWork>()) (fun uow ->
        using (container.Resolve<IChainRepository>(NamedParameter("uow", uow))) (fun chains ->
        using (container.Resolve<IRicRepository>(NamedParameter("uow", uow))) (fun rics -> 
            let chain = chains.FindBy(fun c -> c.Name = "TESTCHAIN").ToList().First()
            chains.Remove chain |> should be (equal 0)
            let ric = rics.FindBy(fun r -> r.Name = "TESTRIC").ToList().First()
            rics.Remove ric |> should be (equal 0)
            uow.Save() |> should be (equal 2)
        )))

    [<Test>]
    let ``Property values simple addition / deletion`` () = 
//        let finish (c : DbTracingContext) = logger.TraceF "Finished : %s %s" (str c.Duration) (c.Command.ToTraceString())
//        let failed (c : DbTracingContext) = logger.ErrorF "Failed : %s %s" (str c.Duration) (c.Command.ToTraceString())
//        DbTracing.Enable(GenericDbTracingListener().OnFinished(Action<_>(finish)).OnFailed(Action<_>(failed)))

        let container = DatabaseBuilder.Container

        // Setting up, adding property

        let [instrumentId] = [MetaTables.BondDescr(BondStructure = "BondStructure", Description = "Description", Ric = "TESTRIC", Currency = "RUB")] 
                             |> createChainRicInstrument container "TESTCHAIN" [|"TESTRIC"|] 
        let propertyId = createProperty "TESTPROP"
            
        // TESTING
        using (container.Resolve<IPropertyValuesRepostiory>()) (fun pvRepo ->
            pvRepo.FindAll().Count() |> should be (equal 0))

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertyValuesRepostiory>(NamedParameter("uow", uow))) (fun pvRepo ->
            let pv = PropertyValue(Value = "12", id_Property = propertyId, id_Instrument = instrumentId)
            pvRepo.Add pv |> should be (equal 0)
            uow.Save () |> should be (equal 1)))

        using (container.Resolve<IPropertyValuesRepostiory>()) (fun pvRepo ->
            pvRepo.FindAll().Count() |> should be (equal 1) )

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertyValuesRepostiory>(NamedParameter("uow", uow))) (fun pvRepo ->
            let pv = pvRepo.FindAll().First()
            pvRepo.Remove pv |> should be (equal 0)
            uow.Save () |> should be (equal 1) ))

        using (container.Resolve<IPropertyValuesRepostiory>()) (fun pvRepo ->
            pvRepo.FindAll().Count() |> should be (equal 0))

        deleteChainRicInstrument ()
        deleteProperty propertyId

    [<Test>]
    let ``Properties simple addition / deletion`` () = 
        let container = DatabaseBuilder.Container

        let cnt = getCount<Property, IPropertiesRepository> ()
        let thePv = ref null
        
        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertiesRepository>(NamedParameter("uow", uow))) (fun pvRepo ->
            let p = Property(Name = "TESTPROP")
            pvRepo.Add p |> should be (equal 0)
            uow.Save () |> should be (equal 1)
            
            thePv := pvRepo.FindBy(fun x -> x.Name = "TESTPROP").First()))

        checkExact<Property, IPropertiesRepository> (cnt + 1)

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertiesRepository>(NamedParameter("uow", uow))) (fun pvRepo ->
            let p = pvRepo.FindById (!thePv).id
            pvRepo.Remove p |> should be (equal 0)
            uow.Save () |> should be (equal 1)
            () ))

        checkExact<Property, IPropertiesRepository> cnt

    [<Test>]
    let ``Property values simultaneous addition / deletion`` () = 
        let container = DatabaseBuilder.Container
        
        let id12 = ref 0L
        let id13 = ref 0L
        let id14 = ref 0L

        let [instrumentId] = [MetaTables.BondDescr(BondStructure = "BondStructure", Description = "Description", Ric = "TESTRIC", Currency = "RUB")] 
                             |> createChainRicInstrument container "TESTCHAIN" [|"TESTRIC"|] 
        let propertyId = createProperty "P1"
        let propertyId2 = createProperty "P2"

        checkZero<PropertyValue, IPropertyValuesRepostiory> ()

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertyValuesRepostiory>(NamedParameter("uow", uow))) (fun pvRepo ->
            let pv = PropertyValue(Value = "12", id_Property = propertyId, id_Instrument = instrumentId)
            pvRepo.Add pv |> should be (equal 0)
            uow.Save () |> should be (equal 1)
            id12 := pv.id
            () ))

        checkExact<PropertyValue, IPropertyValuesRepostiory> 1

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertyValuesRepostiory>(NamedParameter("uow", uow))) (fun pvRepo ->
            let pv1 = PropertyValue(Value = "13", id_Property = propertyId2, id_Instrument = instrumentId)
            pvRepo.Add pv1 |> should be (equal 0)

            let pv = pvRepo.FindAll().First()
            pv.Value <- "14"
            uow.Save () |> should be (equal 2)
            id13 := pvRepo.FindBy(fun pv -> pv.id_Property = propertyId2).First().id
            id14 := pv.id))

        using (container.Resolve<IPropertyValuesRepostiory>()) (fun pvRepo ->
            pvRepo.FindAll().Count() |> should be (equal 2)
            (pvRepo.FindById !id13).Value |> should be (equal "13")
            (pvRepo.FindById !id14).Value |> should be (equal "14"))

        using (container.Resolve<IPropertiesUnitOfWork>()) (fun uow ->
        using (container.Resolve<IPropertyValuesRepostiory>(NamedParameter("uow", uow))) (fun pvRepo ->
            pvRepo.FindAll()
            |> Seq.iter(fun pv -> pvRepo.Remove pv |> should be (equal 0))
            uow.Save () |> should be (equal 2)))

        deleteChainRicInstrument ()
        deleteProperty propertyId
        deleteProperty propertyId2

        checkZero<PropertyValue, IPropertyValuesRepostiory> ()

    [<Test>]
    let ``Recalculating property values on changes in instruments on fake DB`` () =
        // Prepare
        let cnt = ref null
        let builder = ContainerBuilder()

        // IInstrumentTypes
        let instrumentTypesMock = MockRepository.GenerateMock<IInstrumentTypes>()
        RhinoMocksExtensions
            .Stub<_,_>(instrumentTypesMock, Rhino.Mocks.Function<_,_>(fun x -> x.Bond))
            .Return(InstrumentType(id = 1L))
            |> ignore        

        // IPropertyValuesRepostiory
        let propertyValuesRepo = MockRepository.GenerateMock<IPropertyValuesRepostiory>()
        let validProperties = 
            [
                PropertyValue(id_Property = 1L, id_Instrument = 1L, Value = "InstrumentName1 ")
                PropertyValue(id_Property = 1L, id_Instrument = 2L, Value = "InstrumentName2 ")
                PropertyValue(id_Property = 2L, id_Instrument = 1L, Value = "InstrumentName1 0.12 'Jan-20")
                PropertyValue(id_Property = 2L, id_Instrument = 2L, Value = "InstrumentName2 0.23 'Feb-30")
            ]

        let counter = ref 4

        let propertyValues = List.empty<PropertyValue>
        RhinoMocksExtensions
            .Stub<_,_>(propertyValuesRepo, Rhino.Mocks.Function<_,_>(fun x -> x.FindAll()))
            .Return(propertyValues.AsQueryable())
            |> ignore

        RhinoMocksExtensions
            .Stub<_,_>(propertyValuesRepo, Rhino.Mocks.Function<_,_>(fun x -> x.Add(null)))
            .IgnoreArguments()
            .Do(Rhino.Mocks.Function<_,_>(fun (p:PropertyValue) -> 
                logger.InfoF "Got propertyValue <%d / %d / %s>" p.id_Instrument p.id_Property p.Value
                let search = 
                    validProperties 
                    |> List.tryFind (fun property -> 
                        property.id_Instrument = p.id_Instrument && 
                        property.id_Property = p.id_Property && 
                        property.Value = p.Value)
                
                match search with
                | Some _ -> counter := (!counter)-1
                | _ -> ()
                
                0))
            |> ignore      

        RhinoMocksExtensions
            .Stub<_,_>(propertyValuesRepo, Rhino.Mocks.Function<_,_>(fun x -> x.FindBy(null)))
            .IgnoreArguments()
            .Return(propertyValues.AsQueryable())
            |> ignore        

        // IFunctionRegistry
        let funcRegMock = MockRepository.GenerateMock<IFunctionRegistry>()

        let functions = 
            [
             (1L, Evaluatable("$Name + \" \" + $Series")); 
             (2L, Evaluatable("$Name + IIf(Not IsNothing($Coupon), \" \" + Format(\"0.00\", $Coupon), \"\") + IIf(Not IsNothing($Maturity), \" '\" + Format(\"MMM-yy\", $Maturity), \"\")"))
            ] |> Map.ofList |> Map.toDict

        RhinoMocksExtensions
            .Stub<_,_>(funcRegMock, Rhino.Mocks.Function<_,_>(fun x -> x.Add(0L, "")))
            .IgnoreArguments()
            .Return(0)
            |> ignore

        RhinoMocksExtensions
            .Stub<_,_>(funcRegMock, Rhino.Mocks.Function<_,_>(fun x -> x.Items))
            .IgnoreArguments()
            .Return(functions)
            |> ignore

        // IInstrumentDescriptionsReader
        let instrumentDescriptionsReaderMock = MockRepository.GenerateMock<IBondDescriptionsReader>()
        let views = 
            [
             BondDescriptionView(id_Instrument = 1L,id_InstrumentType = 1L,InstrumentName = "InstrumentName1",Coupon = Nullable(0.12),Maturity = Nullable(DateTime(2020, 1, 1)))
             BondDescriptionView(id_Instrument = 2L,id_InstrumentType = 1L,InstrumentName = "InstrumentName2",Coupon = Nullable(0.23),Maturity = Nullable(DateTime(2030, 2, 2)))
            ]
            
        RhinoMocksExtensions
            .Stub<_,_>(instrumentDescriptionsReaderMock, Rhino.Mocks.Function<_,_>(fun x -> x.BondDescriptionViews))
            .Return(views.AsQueryable())
            |> ignore        

        // IPropertiesUnitOfWork
        let propertiesUowMock = MockRepository.GenerateMock<IPropertiesUnitOfWork>()

        builder.RegisterInstance(instrumentTypesMock) |> ignore
        builder.RegisterInstance(propertyValuesRepo) |> ignore
        builder.RegisterInstance(funcRegMock) |> ignore
        builder.RegisterInstance(propertiesUowMock) |> ignore
        builder.RegisterInstance(instrumentDescriptionsReaderMock) |> ignore
        builder.RegisterType<PropertiesUpdater>().As<IPropertiesUpdater>() |> ignore // using original code 
        builder.RegisterInstance(Func<_>(fun () -> !cnt)) |> ignore
        cnt := builder.Build()
        let container = !cnt

        let updater = container.Resolve<IPropertiesUpdater>()
        updater.RecalculateBonds () |> ignore

        !counter |> should be (equal 0)

    [<Test>]
    let ``Recalculating property values on changes in instruments on real DB`` () =
        let finish (c : DbTracingContext) = logger.TraceF "Finished : %s %s" (str c.Duration) (c.Command.ToTraceString())
        let failed (c : DbTracingContext) = logger.ErrorF "Failed : %s %s" (str c.Duration) (c.Command.ToTraceString())
        DbTracing.Enable(GenericDbTracingListener().OnFinished(Action<_>(finish)).OnFailed(Action<_>(failed)))

        let container = DatabaseBuilder.Container
        let [inst1id; instr2id] = 
            [MetaTables.BondDescr(BondStructure = "BondStructure1", Description = "Description1", Ric = "TESTRIC1", Currency = "RUB", ShortName = "Wow1", Series = "S1", Coupon = Nullable 0.12, Maturity = Nullable (DateTime(2020, 1, 1)))
             MetaTables.BondDescr(BondStructure = "BondStructure2", Description = "Description2", Ric = "TESTRIC2", Currency = "RUB", ShortName = "Wow2", Series = "S2", Coupon = Nullable 0.23, Maturity = Nullable (DateTime(2030, 2, 2)))] 
            |> createChainRicInstrument container "TESTCHAIN" [|"TESTRIC1"; "TESTRIC2"|] 

        let registry = container.Resolve<IFunctionRegistry>()
        let updater = container.Resolve<IPropertiesUpdater>()
        let properyReader = container.Resolve<IPropertiesRepository> ()
        let properyValueReader = container.Resolve<IPropertyValuesRepostiory> ()

        properyValueReader.FindAll().Count() |> should be (equal 0)
        
        properyReader
            .FindAll()
            .ToList()
            |> Seq.iter(fun p -> registry.Add(p.id, p.Expression)  |> ignore)

        updater.RecalculateBonds () |> should be (equal 4)

        properyValueReader.FindAll().Count() |> should be (equal 4)

        // Teardown, removing chain and ric
        using (container.Resolve<IChainRicUnitOfWork>()) (fun uow ->
        using (container.Resolve<IChainRepository>(NamedParameter("uow", uow))) (fun chains ->
        using (container.Resolve<IRicRepository>(NamedParameter("uow", uow))) (fun rics -> 
            let chain = chains.FindBy(fun c -> c.Name = "TESTCHAIN").ToList().First()
            chains.Remove chain |> should be (equal 0)
            let ric = rics.FindBy(fun r -> r.Name = "TESTRIC1").ToList().First()
            rics.Remove ric |> should be (equal 0)
            let ric = rics.FindBy(fun r -> r.Name = "TESTRIC2").ToList().First()
            rics.Remove ric |> should be (equal 0)
            uow.Save() |> should be (equal 3)
        )))

    [<Test>]
    let ``Recalculating properties on 0#RUCORP=MM`` () = 
        globalThreshold := LoggingLevel.Info
        let container = DatabaseBuilder.Container
        let br = container.Resolve<IBackupRestore>()
        br.Restore "RUCORP.sql"

        let registry = container.Resolve<IFunctionRegistry>()
        let updater = container.Resolve<IPropertiesUpdater>()
        let properyReader = container.Resolve<IPropertiesRepository> ()

        let nativeKiller = container.Resolve<IPropertyValueCrud> ()
        nativeKiller.DeleteAll()

        properyReader.FindAll().ToList()
        |> Seq.iter (fun x -> registry.Add(x.id, x.Expression) |> ignore)

        let properyValueReader = container.Resolve<IPropertyValuesRepostiory> ()
        properyValueReader.FindAll().Count() |> should be (equal 0)
        updater.RecalculateBonds () |> should be (equal 1846)
        properyValueReader.FindAll().Count() |> should be (equal 1846)
        
        br.Restore "EMPTY.sql"
        globalThreshold := LoggingLevel.Trace

        
    [<Test>]
    let ``Recalculating properties on 0#RUELG=MM`` () = 
        globalThreshold := LoggingLevel.Info
        let container = DatabaseBuilder.Container
        let br = container.Resolve<IBackupRestore>()
        br.Restore "RUELG.sql"

        let registry = container.Resolve<IFunctionRegistry>()
        let updater = container.Resolve<IPropertiesUpdater>()

        let nativeKiller = container.Resolve<IPropertyValueCrud> ()
        nativeKiller.DeleteAll()

        let properyReader = container.Resolve<IPropertiesRepository> ()
        properyReader.FindAll().ToList()
        |> Seq.iter (fun x -> registry.Add(x.id, x.Expression) |> ignore)

        let properyValueReader = container.Resolve<IPropertyValuesRepostiory> ()
        properyValueReader.FindAll().Count() |> should be (equal 0)
        updater.RecalculateBonds () |> should be (equal 96)
        properyValueReader.FindAll().Count() |> should be (equal 96)
        
        br.Restore "EMPTY.sql"
        globalThreshold := LoggingLevel.Trace

module NativeDatabase =
    open YieldMap.Transitive.Native.Entities
    open YieldMap.Tools.Aux
    open System.Collections.Generic
    open YieldMap.Transitive.Native.Crud

    let logger = LogFactory.create "UnitTests.Database"

    [<Test>] 
    let ``Native SQL create 1 instrument`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
            ]

        let sql = helper.BulkInsertSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal ["INSERT INTO Instrument(Name, id_InstrumentType, id_Description) SELECT 'Hello', 1, 2"])

    [<Test>] 
    let ``Native SQL delete 1 instrument`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
            ]

        let sql = helper.BulkDeleteSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal
            [
                "BEGIN TRANSACTION;\nDELETE FROM Instrument  WHERE Name = 'Hello' AND id_InstrumentType = 1 AND id_Description = 2;\nEND TRANSACTION;"
            ])

    [<Test>] 
    let ``Native SQL delete 2 instruments`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
            ]

        let sql = helper.BulkDeleteSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal 
            [
            "BEGIN TRANSACTION;\nDELETE FROM Instrument  WHERE Name = 'Hello' AND id_InstrumentType = 1 AND id_Description = 2;\nDELETE FROM Instrument  WHERE Name = 'Bye' AND id_InstrumentType = 2 AND id_Description = NULL;\nEND TRANSACTION;"
            ])

    [<Test>] 
    let ``Native SQL create 2 instruments`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
            ]

        let sql = helper.BulkInsertSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal 
            ["INSERT INTO Instrument(Name, id_InstrumentType, id_Description) SELECT 'Hello', 1, 2 UNION SELECT 'Bye', 2, NULL"])

    [<Test>] 
    let ``Native SQL update 1 instrument`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(id = 15L, Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
            ]

        let sql = helper.BulkUpdateSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal ["BEGIN TRANSACTION;\n UPDATE Instrument SET Name = 'Hello', id_InstrumentType = 1, id_Description = 2 WHERE id = 15;\nEND TRANSACTION;"])

    [<Test>] 
    let ``Native SQL update 2 instruments`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(id = 15L, Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(id = 16L, Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
            ]

        let sql = helper.BulkUpdateSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal ["BEGIN TRANSACTION;\n UPDATE Instrument SET Name = 'Hello', id_InstrumentType = 1, id_Description = 2 WHERE id = 15;\nUPDATE Instrument SET Name = 'Bye', id_InstrumentType = 2, id_Description = NULL WHERE id = 16;\nEND TRANSACTION;"])

    [<Test>] 
    let ``Native SQL delete 1 instrument by id`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(id = 15L, Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
            ]

        let sql = helper.BulkDeleteSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal
            [
                "DELETE FROM Instrument  WHERE id IN (15)"
            ])

    [<Test>] 
    let ``Native SQL delete 2 instruments by id`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(id = 15L, Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(id = 16L, Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
            ]

        let sql = helper.BulkDeleteSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal 
            [
                "DELETE FROM Instrument  WHERE id IN (15, 16)"
            ])

    [<Test>] 
    let ``Native SQL delete mixed`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<INEntityHelper>()

        let instruments = 
            [
                NInstrument(id = 15L, Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(id = 16L, Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
                NInstrument(Name = "Hello", id_InstrumentType = Nullable 1L, id_Description = Nullable 2L)
                NInstrument(Name = "Bye", id_InstrumentType = Nullable 2L, id_Description = Nullable())
            ]

        let sql = helper.BulkDeleteSql<NInstrument>(instruments)
        logger.InfoF "Got sql %A" (List.ofSeq sql)
        sql |> should be (equal 
            [
                "DELETE FROM Instrument  WHERE id IN (15, 16)"
                "BEGIN TRANSACTION;\n UPDATE Instrument SET Name = 'Hello', id_InstrumentType = 1, id_Description = 2 WHERE id = 15;\nUPDATE Instrument SET Name = 'Bye', id_InstrumentType = 2, id_Description = NULL WHERE id = 16;\nEND TRANSACTION;"
            ])

    [<Test>]
    let ``Mutable keys`` () =
        let x = Dictionary<NProperty, int> ()
        let mutable key = NProperty(id = 1L)
        x.Add(key, 1)
        key.id <- 2L
        x.[key] |> should be (equal 1L)


    [<Test>]
    let ``Simple read`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<IFeedCrud>()
        helper.FindAll().Count() |> should be (greaterThan 0)

    [<Test>]
    let ``Add, read id, update and then delete`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<IFeedCrud>()
        helper.FindAll().Count() |> should be (equal 1)

        let newFeed = NFeed(Name = "A", Description = "Description")
        newFeed.id |> should be (equal 0L)
        helper.Create newFeed
        helper.Save<NFeed>()
        
        helper.FindAll().Count() |> should be (equal 2)
        newFeed.id |> should be (greaterThan 0L)

        newFeed.Description <- "Advanced description"
        helper.Update newFeed
        helper.Save<NFeed>()
        helper.FindAll().Count() |> should be (equal 2)

        let helper2 = container.Resolve<IFeedCrud>()
        helper2.FindById(newFeed.id).Description |> should be (equal "Advanced description")

        helper.Delete newFeed
        helper.Save<NFeed> ()

        helper.FindAll().Count() |> should be (equal 1)


    [<Test>]
    let ``Add 2, read ids, update and then delete`` () =
        let container = DatabaseBuilder.Container 
        let helper = container.Resolve<IFeedCrud>()
        helper.FindAll().Count() |> should be (equal 1)

        let newFeed1 = NFeed(Name = "A", Description = "Description")
        let newFeed2 = NFeed(Name = "B", Description = "Description B")
        newFeed1.id |> should be (equal 0L)
        newFeed2.id |> should be (equal 0L)
        helper.Create newFeed1
        helper.Create newFeed2
        helper.Save<NFeed>()
        
        helper.FindAll().Count() |> should be (equal 3)
        newFeed1.id |> should be (greaterThan 0L)
        newFeed2.id |> should be (greaterThan 0L)

        newFeed1.Description <- "Advanced description"
        helper.Update newFeed1
        helper.Save<NFeed>()
        helper.FindAll().Count() |> should be (equal 3)

        let helper2 = container.Resolve<IFeedCrud>()
        helper2.FindById(newFeed1.id).Description |> should be (equal "Advanced description")

        helper.Delete newFeed1
        helper.Delete newFeed2
        let newFeed3 = NFeed(Name = "C", Description = "Description C")
        helper.Create newFeed3
        helper.Save<NFeed> ()
        newFeed3.id |> should be (greaterThan 0L)

        helper.FindAll().Count() |> should be (equal 2)
        newFeed3.id <- 0L
        helper.Delete newFeed3
        helper.Save<NFeed> ()
        helper.FindAll().Count() |> should be (equal 1)
