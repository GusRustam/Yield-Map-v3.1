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
        try 
            f ()
            failwith "No error"
        with :? LexicalException as e ->
            e.Data0.position

    [<Test>]
    let ``Parsing values: numbers (int and double)`` () =
        Lexem.parse "1" |> should be (equal [0, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "   1   " |> should be (equal [3, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1   " |> should be (equal [0, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1 2" |> should be (equal 
            [
                0, Lexem.Value <| Value.Integer 1L
                2, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "   1   2 " |> should be (equal 
            [
                3, Lexem.Value <| Value.Integer 1L
                7, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2" |> should be (equal 
            [
                0, Lexem.Value <| Value.Double 1.0
                4, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2.4" |> should be (equal 
            [
                0, Lexem.Value <| Value.Double 1.0
                4, Lexem.Value <| Value.Double 2.4
            ])

        // TODO TESTS ON IVALID DATA
        // TODO TESTS ON ALL LEXEMS
        
        // TODO MIXED TESTS lexCatch (fun () -> Lexem.parse "1e" |> ignore) |> should be (equal 0) 

    [<Test>]
    let ``Parsing values: dates`` () =
        Lexem.parse "#1/2/2013#" |> should be (equal [0, Lexem.Value <| Value.Date (DateTime(2013,2,1))])
        Lexem.parse "  #21/12/2013#  " |> should be (equal [2, Lexem.Value <| Value.Date (DateTime(2013,12,21))])

    [<Test>]
    let ``Parsing values: booleans`` () =
        Lexem.parse "true" |> should be (equal [0, Lexem.Value <| Value.Bool true])
        Lexem.parse "false" |> should be (equal [0, Lexem.Value <| Value.Bool false])
        Lexem.parse "  False  FALSE   TRUE   tRuE" |> should be (equal 
            [
                2, Lexem.Value <| Value.Bool false
                9, Lexem.Value <| Value.Bool false
                17, Lexem.Value <| Value.Bool true
                24, Lexem.Value <| Value.Bool true
            ])

    [<Test>]
    let ``Parsing values: ratings`` () =
        Lexem.parse "[BBB+]" |> should be (equal [0, Lexem.Value <| Value.Rating "BBB+"])
        Lexem.parse "[]" |> should be (equal [0, Lexem.Value <| Value.Rating ""])
        Lexem.parse "  [sdf]  [AAA]   [wow!]   [-+-+]" |> should be (equal 
            [
                2, Lexem.Value <| Value.Rating "sdf"
                9, Lexem.Value <| Value.Rating "AAA"
                17, Lexem.Value <| Value.Rating "wow!"
                26, Lexem.Value <| Value.Rating "-+-+"
            ])

    [<Test>]
    let ``Parsing values: strings`` () =
        Lexem.parse "\"hello world!\"" |> should be (equal [0, Lexem.Value <| Value.String "hello world!"])
        Lexem.parse "\"Hello World!\"" |> should be (equal [0, Lexem.Value <| Value.String "Hello World!"])
        Lexem.parse "\"Hello    World!\"" |> should be (equal [0, Lexem.Value <| Value.String "Hello    World!"])
        Lexem.parse "\"hello world!\"     \"hello world!\"            \"Hello    World!\" \"Hello World!\"       " |> should be (equal 
            [
                0, Lexem.Value <| Value.String "hello world!"
                19, Lexem.Value <| Value.String "hello world!"
                45, Lexem.Value <| Value.String "Hello    World!"
                63, Lexem.Value <| Value.String "Hello World!"
            ])
        Lexem.parse "\"hello world!\"\"hello world!\""|> should be (equal 
            [
                0, Lexem.Value <| Value.String "hello world!"
                14, Lexem.Value <| Value.String "hello world!"
            ])
    
    [<Test>]
    let ``Parsing lexems: brackets`` () =
        Lexem.parse "(() ) (  " |> should be (equal 
            [
                0, Lexem.OpenBracket
                1, Lexem.OpenBracket
                2, Lexem.CloseBracket
                4, Lexem.CloseBracket
                6, Lexem.OpenBracket
            ])

    [<Test>]
    let ``Parsing lexems: variables`` () =
        // valid variable names
        Lexem.parse "$a" |> should be (equal [0, Lexem.Variable <| Variable.Global "A"])
        Lexem.parse "$a.b" |> should be (equal [0, Lexem.Variable <| Variable.Object ("A", "B")])
        Lexem.parse "$a.b $F $eee_11.a23" |> should be (equal 
            [
                0, Lexem.Variable <| Variable.Object ("A", "B")
                5, Lexem.Variable <| Variable.Global "F"
                8, Lexem.Variable <| Variable.Object ("EEE_11", "A23")
            ])
        
        // invalid variable names
        lexCatch (fun () -> Lexem.parse "$a.b $c $DDDD.312r4wefr3" |> ignore) |> should be (equal 13)
        lexCatch (fun () -> Lexem.parse "$1" |> ignore) |> should be (equal 0)
        lexCatch (fun () -> Lexem.parse "$a $_" |> ignore) |> should be (equal 3)

    [<Test>]
    let ``Parsing lexems: function calls`` () =
        Lexem.parse "Hello()" |> should be (equal [0, Lexem.FunctionCall <| { name = "HELLO"; parameters = [] }])
        Lexem.parse "Hello(12)" |> should be (equal 
            [
                0, Lexem.FunctionCall  
                    { 
                        name = "HELLO"; parameters = 
                            [
                                6, Lexem.Value <| Value.Integer 12L
                            ] 
                    }
            ])
        Lexem.parse "OhMyGod(12, 23, $a, true)" |> should be (equal 
            [
                0, Lexem.FunctionCall  
                    { 
                        name = "OHMYGOD"; parameters = 
                            [
                                8+0, Lexem.Value <| Value.Integer 12L
                                8+4, Lexem.Value <| Value.Integer 23L
                                8+8, Lexem.Variable <| Variable.Global "A"
                                8+12, Lexem.Value <| Value.Bool true
                            ] 
                    }
            ])
        Lexem.parse "OhMyGod(Hello(12), Bye(23, $alpha.beta_11), $a, false)" |> should be (equal 
            [ 0, Lexem.FunctionCall  
                { name = "OHMYGOD"; parameters = 
                    [   8, Lexem.FunctionCall { name = "HELLO"; parameters = [14, Lexem.Value <| Value.Integer 12L] }
                        19, Lexem.FunctionCall 
                            { name = "BYE"; parameters = 
                                [
                                    23, Lexem.Value <| Value.Integer 23L
                                    27, Lexem.Variable <| Variable.Object ("ALPHA", "BETA_11")
                                ]}
                        44, Lexem.Variable <| Variable.Global "A"
                        48, Lexem.Value <| Value.Bool false
                    ]}])

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