namespace YieldMap.Tests.Unit

open Autofac
open Rhino.Mocks
open FsUnit
open NUnit.Framework

open YieldMap.Database
open YieldMap.Transitive.Registry
open YieldMap.Language.Lexan
open YieldMap.Tools.Testability
open YieldMap.Tools.Aux

open System.Data.Entity
open System.Linq
open System.Collections.Generic

module Registry = 
    let mutable registry = Unchecked.defaultof<IFunctionRegistry>
    let builder = ContainerBuilder()
    builder.RegisterType<FunctionRegistry>().As<IFunctionRegistry>() |> ignore // .SingleInstance()
    let container = builder.Build()
    
    [<SetUp>]
    let setup () = registry <- container.Resolve<IFunctionRegistry> () // TODO CONTAINER

    [<Test>]
    let ``Empty list test`` () = 
        let propertyList = []
        registry.Add propertyList
        registry.Items |> Map.fromDict |> Map.isEmpty |> should be (equal true)

    [<Test>]
    let ``Evaluating simple expressions`` () = 
        let propertyList = [(1L, "1+1")]
        registry.Add propertyList
        registry.Items |> Map.fromDict  |> Map.isEmpty |> should be (equal false)
        registry.Evaluate (1L, Dictionary<_,_>()) |> should be (equal (Value.Integer 2L))

    [<Test>]
    let ``Adding and removing expressions`` () = 
        registry.Add [(1L, "1+1")]
        registry.Items |> Map.fromDict  |> Map.isEmpty |> should be (equal false)
        registry.Add [(1L, "1+3")]
        registry.Evaluate  (1L, Dictionary<_,_>()) |> should be (equal (Value.Integer 4L))
        registry.Add []
        registry.Items |> Map.fromDict  |> Map.isEmpty |> should be (equal false)
        registry.Add [(1L, "1+6")]
        registry.Evaluate (1L, Dictionary<_,_>()) |> should be (equal (Value.Integer 7L))

    [<Test>]
    let ``Evaluating complex expressions`` () = 
        let propertyList = [(1L, "$a1+1")]
        registry.Add propertyList
        registry.Items |> Map.fromDict  |> Map.isEmpty |> should be (equal false)
        registry.Evaluate (1L, Dictionary<_,_>()) |> should be (equal (Value.Nothing))
        
        let a = Dictionary<_,_>()
        a.Add("A1", box 2)

        registry.Evaluate (1L, a) |> should be (equal (Value.Integer 3L))


    [<Test>]
    let ``Saving into database`` () =
        let propertyList = [(1L, "$a1+1")]
        registry.Add propertyList
        registry.Items |> Map.fromDict |> Map.isEmpty |> should be (equal false)
        registry.Evaluate (1L, Dictionary<_,_>())  |> should be (equal (Value.Nothing))

        let a = Dictionary<_,_>()
        a.Add("A1", box 2)
        registry.Evaluate (1L, a) |> should be (equal (Value.Integer 3L))
        
        let save = ref 0
        let inc () = save := !save + 1

//        let mock = MockRepository.GenerateMock<IPropertiesUpdater>()
//        RhinoMocksExtensions.Stub<_,_>(mock, Rhino.Mocks.Function<_,_>(fun x -> x.Recalculate()))
//            .Return(true) 
//            .Callback(fun () -> inc (); true)
//            |> ignore
//        let storage = mock
//        
//        let c = ContainerBuilder()
//        c.RegisterInstance(storage) |> ignore
//        c.Update(container)
//
//        let saver = container.Resolve<IPropertiesUpdater> ()

        let saver = { new IPropertiesUpdater with member x.Recalculate() = inc (); true}

        saver.Recalculate ()  |> ignore      

        !save |> should be (equal 1)