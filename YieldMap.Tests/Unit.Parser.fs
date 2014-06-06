namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module Parser = 
    open System.Collections.Generic

    open YieldMap.Parser
    open YieldMap.Tools.Logging
   
    let logger = LogFactory.create "UnitTests.Parser"

    let error expr l = 
        let p = Parser()
        try
            let grammar = p.Parse expr
            grammar.Count |> should be (equal l)
            None
        with :? Exceptions.ParserException as e ->
            logger.ErrorEx "" e
            Some e.ErrorPos


    let eval expr = 
        let p = Parser()
        try
            let grammar = p.Parse expr
            let i = Interpreter grammar
            Some (i.Evaluate <| Dictionary<_,_>())
            
        with :? Exceptions.ParserException as e ->
            logger.ErrorEx "" e
            None

    [<Test>]
    let ``Single-term expressions and spaces`` () =
        error "$a = 2" 3 |> should be (equal None) 
        error "$a= 2" 3 |> should be (equal None) 
        error "$a =2" 3 |> should be (equal None)
        error "$a=2" 3 |> should be (equal None)
        error "($a = 2)" 3 |> should be (equal None)
        error "($a= 2)" 3 |> should be (equal None)
        error "($a =2)" 3 |> should be (equal None)
        error "($a=2)" 3 |> should be (equal None)
        error "( $a = 2)" 3 |> should be (equal None)
        error "( $a= 2)" 3 |> should be (equal None)
        error "( $a =2)" 3 |> should be (equal None)
        error "( $a=2)" 3 |> should be (equal None)
        error "($a = 2 )" 3 |> should be (equal None)
        error "($a= 2 )" 3 |> should be (equal None)
        error "($a =2 )" 3 |> should be (equal None)
        error "($a=2 )" 3 |> should be (equal None)
        error "($a = 2) " 3 |> should be (equal None)
        error "($a= 2) " 3 |> should be (equal None)
        error "($a =2) " 3 |> should be (equal None)
        error "($a=2) " 3 |> should be (equal None)

    [<Test>]
    let ``Single-term calculations`` () =
        eval "2+2 = 4" |> should be (equal (Some true))                