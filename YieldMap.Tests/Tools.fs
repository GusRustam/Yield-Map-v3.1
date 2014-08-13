namespace YieldMap.Tests.Tools

open Autofac

open NUnit.Framework
open FsUnit

open System
open System.IO
open System.Linq

open YieldMap.Tools.Location
open YieldMap.Tools.Logging

open YieldMap.Core.DbManager

module Tools =
    open YieldMap.Transitive.Procedures
    open YieldMap.Loader.Calendar
    open YieldMap.Transitive.Native
    open YieldMap.Transitive.Native.Entities
    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.MetaChains
    open YieldMap.Core.Startup

    let logger = LogFactory.create "Tools"
    let container = YieldMap.Transitive.DatabaseBuilder.Container

    let mutable (c : Calendar) = Unchecked.defaultof<Calendar>
    let mutable (s : Drivers) = Unchecked.defaultof<Drivers>

    let init chains dt = 
        c <- MockCalendar dt

        use feeds = container.Resolve<ICrud<NFeed>>()
        let mutable feed = feeds.FindById 1L

        if feed  = null then
            feed <- NFeed(Name = "Q")
            feeds.Create feed |> ignore
            feeds.Save () |> ignore

        use repo = container.Resolve<ICrud<NChain>>()
        let theFeed = feed

        chains |> Array.iter (fun name -> 
            if not <| repo.FindBy(fun (x:NChain) -> x.Name = name).Any() then
                repo.Create <| NChain(Name = name, id_Feed = Nullable(theFeed.id), Params = "") |> ignore)
        repo.Save () |> ignore 

        s <- {
            Factory = MockFactory ()
            TodayFix = dt
            Loader = MockChainMeta c
            Calendar = c
            DbServices = container
        }

        Startup s


    let command cmd func state = 
        logger.InfoF "===> %s " cmd
        let res = func () |> Async.RunSynchronously  
        logger.InfoF " <=== %s : %s " cmd (res.ToString())
        res |> should be (equal state)

    [<Test>]
    let ``Restore DB to defaults`` () =
        let x = container.Resolve<IBackupRestore>();
        x.Restore (Path.Combine(Location.path, "../../EMPTY.sql"))

    [<Test>]
    let ``Backup current database`` () =
        let br = container.Resolve<IBackupRestore>()
        br.Backup() |> logger.Info            


    [<Test>]
    let ``Restore DB to 0#RUCORP=MM`` () =
        let x = container.Resolve<IBackupRestore>();
        x.Restore (Path.Combine(Location.path, "../../RUCORP.sql"))

    [<Test>]
    let ``Restore DB to 0#RUELG=MM`` () =
        let x = container.Resolve<IBackupRestore>();
        x.Restore (Path.Combine(Location.path, "../../RUELG.sql"))

    (* ========================= ============================= *)
    [<Test>]
    let ``Initialize Db with 0#RUCORP=MM`` () =
        let dt = DateTime(2014,5,14) 
        let br = container.Resolve<IBackupRestore>()
        br.Restore "EMPTY.sql"
        
        let x = init [|"0#RUCORP=MM"|] dt

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        
        br.Backup() |> logger.Info

    (* ========================= ============================= *)
    [<Test>]
    let ``Initialize Db with 0#RUELG=MM`` () =
        let dt = DateTime(2014,5,14) 
        let br = container.Resolve<IBackupRestore>()
        br.Restore "EMPTY.sql"

        let x = init [|"0#RUELG=MM"|] dt

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        
        br.Backup() |> logger.Info            
            
    (* ========================= ============================= *)
    [<Test>]
    let ``Initialize Db with 0#AM097464227=`` () =
        let dt = DateTime(2014,5,14) 
        let br = container.Resolve<IBackupRestore>()
        br.Restore "EMPTY.sql"

        let x = init [|"0#AM097464227="|] dt

        command "Connect" x.Connect (State Connected)
        command "Reload" (fun () -> x.Reload (true, 100000000)) (State Initialized)
        
        br.Backup() |> logger.Info