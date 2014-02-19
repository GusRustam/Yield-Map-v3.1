#if INTERACTIVE
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map\Tools\YieldMapDsl\packages\xunit.1.9.2\lib\net20\xunit.dll"
#load "common-testing.fsx"
#endif

open System
open System.IO
open System.Xml

open YieldMap.Data.Answers
open YieldMap.Data.Requests
open YieldMap.Data.Loading
open YieldMap.Data.MetaTables
open YieldMap.Data

open Xunit

open ``Common-testing``

[<Fact>]
let ``retrieving-mock-data`` = 
    let q = MockLoader() :> MetaLoader
    Dex2Tests.test q |> Async.Start