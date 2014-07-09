namespace YieldMap.Tests.Tools

open Autofac

open NUnit.Framework
open FsUnit

open System
open System.IO

open YieldMap.Tools.Location
open YieldMap.Tools.Logging

open YieldMap.Core.DbManager

module Tools =
    let logger = LogFactory.create "Tools"
    let container = YieldMap.Transitive.DatabaseBuilder.Container

    [<Test>]
    let ``Restore DB to defaults`` () =
        DbManager container
        |> DbManager.restore (Path.Combine(Location.path, "../../EMPTY.sql"))


    [<Test>]
    let ``Restore DB to RUCORP`` () =
        DbManager container
        |> DbManager.restore (Path.Combine(Location.path, "../../RUCORP.sql"))