namespace YieldMap.Tests.Tools

open System
open System.IO

open NUnit.Framework
open FsUnit

open YieldMap.Database
open YieldMap.Tools.Location
open YieldMap.Tools.Logging


module Tools =
    let logger = LogFactory.create "Tools"

    [<Test>]
    let ``Restore DB to defaults`` () =
        StoredProcedures.BackupRestore.Restore <| Path.Combine(Location.path, "../../EMPTY.sql")
