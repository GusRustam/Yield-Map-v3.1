namespace YieldMap.Tests.Unit

open Autofac
open Foq
open FsUnit
open NUnit.Framework

open YieldMap.Core.Container
open YieldMap.Core.Register
open YieldMap.Database
open YieldMap.Database.Access
open YieldMap.Database.Procedures.Additions
open YieldMap.Database.Tools
open YieldMap.Language.Lexan
open YieldMap.Tools.Testability

open System.Data.Entity
open System.Linq
open System.Collections.Generic

module Registry = 
    let mutable registry = Unchecked.defaultof<Registry>
    
    [<SetUp>]
    let setup () = registry <- container.Resolve<Registry> ()

    [<Test>]
    let ``Empty list test`` () = 
        let propertyList = []
        registry.Add propertyList
        registry.Items () |> Map.isEmpty |> should be (equal true)

    [<Test>]
    let ``Evaluating simple expressions`` () = 
        let propertyList = [(1L, "1+1")]
        registry.Add propertyList
        registry.Items () |> Map.isEmpty |> should be (equal false)
        registry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 2L))

    [<Test>]
    let ``Adding and removing expressions`` () = 
        registry.Add [(1L, "1+1")]
        registry.Items () |> Map.isEmpty |> should be (equal false)
        registry.Add [(1L, "1+3")]
        registry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 4L))
        registry.Add []
        registry.Items () |> Map.isEmpty |> should be (equal false)
        registry.Add [(1L, "1+6")]
        registry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 7L))

    [<Test>]
    let ``Evaluating complex expressions`` () = 
        let propertyList = [(1L, "$a1+1")]
        registry.Add propertyList
        registry.Items () |> Map.isEmpty |> should be (equal false)
        registry.Evaluate 1L Map.empty |> should be (equal (Value.Nothing))
        registry.Evaluate 1L ([("A1", box 2)] |> Map.ofList) |> should be (equal (Value.Integer 3L))

//    type MyDbSet<'T> = 
//        member __.Create<'U when 'U : not struct and 'U :> 'T> () : 'U  = Unchecked.defaultof<'U>
////        interface 'T IDbSet with 
////            member __.Find x = Unchecked.defaultof<'T>
////            member __.Add x = x
////            member __.Remove x = x
////            member __.Attach x = x
////            member __.get_Local () = failwith ""
////            member __.Create () = Unchecked.defaultof<'T>
////            member __.Create<'U when 'U : not struct and 'U :> 'T> ()  = Unchecked.defaultof<'U>



    [<Test>]
    let ``Saving into database`` () =
        let propertyList = [(1L, "$a1+1")]
        registry.Add propertyList
        registry.Items () |> Map.isEmpty |> should be (equal false)
        registry.Evaluate 1L Map.empty |> should be (equal (Value.Nothing))
        registry.Evaluate 1L ([("A1", box 2)] |> Map.ofList) |> should be (equal (Value.Integer 3L))
        

//        let storage = 
//            Mock<IPropertyStorage>()
//            |> Foq.setup (fun x -> <@ x.Save(any()) @>)
//            |> Foq.returnsCall (fun () -> save := !save + 1)
//            |> Foq.create          

        let save = ref 0
        let inc () = save := !save + 1
        let storage = Mock<IPropertyStorage>()
        let storage = storage.Setup(fun x -> <@ x.Save(any()) @>)
        let storage = storage.Calls<unit>(inc)
        let storage = storage.Create()

        let conn = Mock<IDbConn>()
                   |> Foq.setup (fun x -> <@ x.CreateContext() @>)
                   |> Foq.returns null
                   |> Foq.create

        let c = ContainerBuilder()
        c.RegisterInstance(storage) |> ignore
        c.RegisterInstance(conn) |> ignore
        c.Update(container)

        let saver = container.Resolve<Recounter> ()
        saver.Recount ()        

        !save |> should be (equal 1)