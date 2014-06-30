namespace YieldMap.Tests.Unit

open NUnit.Framework
open FsUnit

open YieldMap.Core.Register
open YieldMap.Language.Lexan

open System.Data.Entity
open System.Linq
open System.Collections.Generic

module Registry = 
    [<SetUp>]
    let setup () = 
        defaultRegistry.Clear ()

    [<Test>]
    let ``Empty list test`` () = 
        let propertyList = []
        defaultRegistry.Refresh propertyList
        defaultRegistry.Items () |> Map.isEmpty |> should be (equal true)

    [<Test>]
    let ``Evaluating simple expressions`` () = 
        let propertyList = [(1L, "1+1")]
        defaultRegistry.Refresh propertyList
        defaultRegistry.Items () |> Map.isEmpty |> should be (equal false)
        defaultRegistry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 2L))

    [<Test>]
    let ``Adding and removing expressions`` () = 
        defaultRegistry.Refresh [(1L, "1+1")]
        defaultRegistry.Items () |> Map.isEmpty |> should be (equal false)
        defaultRegistry.Refresh [(1L, "1+3")]
        defaultRegistry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 4L))
        defaultRegistry.Refresh []
        defaultRegistry.Items () |> Map.isEmpty |> should be (equal false)
        defaultRegistry.Refresh [(1L, "1+6")]
        defaultRegistry.Evaluate 1L Map.empty |> should be (equal (Value.Integer 7L))

    [<Test>]
    let ``Evaluating complex expressions`` () = 
        let propertyList = [(1L, "$a1+1")]
        defaultRegistry.Refresh propertyList
        defaultRegistry.Items () |> Map.isEmpty |> should be (equal false)
        defaultRegistry.Evaluate 1L Map.empty |> should be (equal (Value.Nothing))
        defaultRegistry.Evaluate 1L ([("A1", box 2)] |> Map.ofList) |> should be (equal (Value.Integer 3L))