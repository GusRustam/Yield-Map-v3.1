namespace YieldMap.Tests.Tools

open System
open System.IO

open NUnit.Framework
open FsUnit

open YieldMap.Tools.Location
open YieldMap.Tools.Logging

open YieldMap.Core.DbManager


module Tools =
    let logger = LogFactory.create "Tools"

    [<Test>]
    let ``Restore DB to defaults`` () =
        DbManager () 
        |> DbManager.restore (Path.Combine(Location.path, "../../EMPTY.sql"))


    [<Test>]
    let ``Restore DB to RUCORP`` () =
        DbManager () 
        |> DbManager.restore (Path.Combine(Location.path, "../../RUCORP.sql"))