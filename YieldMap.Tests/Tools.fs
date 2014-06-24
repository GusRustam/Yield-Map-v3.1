namespace YieldMap.Tests.Tools

open System
open System.IO

open NUnit.Framework
open FsUnit

open YieldMap.Tools.Location
open YieldMap.Tools.Logging

open YieldMap.Core.Manager


module Tools =
    let logger = LogFactory.create "Tools"

    [<Test>]
    let ``Restore DB to defaults`` () =
        Manager () 
        |> Manager.restore (Path.Combine(Location.path, "../../EMPTY.sql"))
