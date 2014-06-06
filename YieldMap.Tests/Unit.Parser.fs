namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module Parser = 
    open YieldMap.Parser
    open YieldMap.Tools.Logging
   
    let logger = LogFactory.create "UnitTests.Parser"

    let error expr = 
        let p = Parser()
        try
            p.SetFilter expr |> ignore
            None
        with :? Exceptions.ParserException as e ->
            logger.ErrorEx "" e
            Some e.ErrorPos

    [<Test>]
    let ``Simple Error`` () =
        error "$a = 2" |> should be (equal None)
        error "$a= 2" |> should be (equal None)
        error "$a =2" |> should be (equal None)
        error "$a=2" |> should be (equal None)
        