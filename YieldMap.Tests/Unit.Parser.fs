namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module Language = 
    open System.Collections.Generic

    open YieldMap.Language
    open YieldMap.Language.Analyzer
    open YieldMap.Tools.Logging
   
    let logger = LogFactory.create "UnitTests.Language"

    let lexCatch f = 
        try f ()
            failwith "No error"
        with :? LexicalException as e ->
            e.Data1

    [<Test>]
    let ``Parsing numbers`` () =
        Lexem.parse "1" |> should be (equal [Lexem.Value <| Value.Integer 1L])
        Lexem.parse "   1   " |> should be (equal [Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1" |> should be (equal [Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1 2" |> should be (equal 
            [
                Lexem.Value <| Value.Integer 1L
                Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "   1   2 " |> should be (equal 
            [
                Lexem.Value <| Value.Integer 1L
                Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2" |> should be (equal 
            [
                Lexem.Value <| Value.Double 1.0
                Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2.4" |> should be (equal 
            [
                Lexem.Value <| Value.Double 1.0
                Lexem.Value <| Value.Double 2.4
            ])

        // TODO TESTS ON IVALID DATA
        // TODO TESTS ON ALL LEXEMS
        // TODO AND THEN - THE PARSER
        
        lexCatch (fun () -> Lexem.parse "1e" |> ignore) |> should be (equal 0)

    [<Test>]
    let ``Parsing dates`` () =
        Lexem.parse "#1/2/2013#" |> should be (equal [Lexem.Value <| Value.Date (DateTime(2013,2,1))])
        Lexem.parse "  #21/12/2013#  " |> should be (equal [Lexem.Value <| Value.Date (DateTime(2013,12,21))])

    [<Test>]
    let ``Parsing booleans`` () =
        Lexem.parse "true" |> should be (equal [Lexem.Value <| Value.Bool true])
        Lexem.parse "false" |> should be (equal [Lexem.Value <| Value.Bool false])
        Lexem.parse "  False  FALSE   TRUE   tRuE" |> should be (equal 
            [
                Lexem.Value <| Value.Bool false
                Lexem.Value <| Value.Bool false
                Lexem.Value <| Value.Bool true
                Lexem.Value <| Value.Bool true
            ])

    [<Test>]
    let ``Parsing string`` () =
        Lexem.parse "\"hello world!\"" |> should be (equal [Lexem.Value <| Value.String "hello world!"])
        Lexem.parse "\"Hello World!\"" |> should be (equal [Lexem.Value <| Value.String "Hello World!"])
        Lexem.parse "\"Hello    World!\"" |> should be (equal [Lexem.Value <| Value.String "Hello    World!"])
        Lexem.parse "\"hello world!\"     \"hello world!\"            \"Hello    World!\" \"Hello World!\"       " |> should be (equal 
            [
                Lexem.Value <| Value.String "hello world!"
                Lexem.Value <| Value.String "hello world!"
                Lexem.Value <| Value.String "Hello    World!"
                Lexem.Value <| Value.String "Hello World!"
            ])

        lexCatch (fun () -> Lexem.parse "\"hello world!\"\"hello world!\"" |> ignore) |> should be (equal 15)
        
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
    let ``Single-term expressions, spaces and brackets`` () =
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