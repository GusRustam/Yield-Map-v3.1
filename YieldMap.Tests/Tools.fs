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
    open YieldMap.Transitive.Procedures

    let logger = LogFactory.create "Tools"
    let container = YieldMap.Transitive.DatabaseBuilder.Container

    [<Test>]
    let ``Restore DB to defaults`` () =
        let x = container.Resolve<IBackupRestore>();
        x.Restore (Path.Combine(Location.path, "../../EMPTY.sql"))


    [<Test>]
    let ``Restore DB to RUCORP`` () =
        let x = container.Resolve<IBackupRestore>();
        x.Restore (Path.Combine(Location.path, "../../RUCORP.sql"))