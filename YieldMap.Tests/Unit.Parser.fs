namespace YieldMap.Tests.Unit

open System

open NUnit.Framework
open FsUnit

module Language = 
    open System.Collections.Generic

    open YieldMap.Language
    open YieldMap.Language.Lexan
    open YieldMap.Language.Syntan
    open YieldMap.Language.Exceptions

    open YieldMap.Tools.Aux
    open YieldMap.Tools.Ratings
    open YieldMap.Tools.Logging
   
    let logger = LogFactory.create "UnitTests.Language"

    let errorAt str = 
        try 
            Lexem.parse str |> ignore
            failwith "No error"
        with :? GrammarException as e ->
            e.Data0.position

    [<Test>]
    let ``Parsing values: numbers (int and double)`` () =
        Lexem.parse "1" |> fst |> should be (equal [0, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "   1   "|> fst |> should be (equal [3, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1   " |> fst |> should be (equal [0, Lexem.Value <| Value.Integer 1L])
        Lexem.parse "1 2" |> fst |> should be (equal 
            [
                0, Lexem.Value <| Value.Integer 1L
                2, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "   1   2 " |> fst |> should be (equal 
            [
                3, Lexem.Value <| Value.Integer 1L
                7, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2" |> fst |> should be (equal 
            [
                0, Lexem.Value <| Value.Double 1.0
                4, Lexem.Value <| Value.Integer 2L
            ])
        Lexem.parse "1.0 2.4" |> fst |> should be (equal 
            [
                0, Lexem.Value <| Value.Double 1.0
                4, Lexem.Value <| Value.Double 2.4
            ])

    [<Test>]
    let ``Parsing values: dates`` () =
        Lexem.parse "#1/2/2013#" |> fst |> should be (equal [0, Lexem.Value <| Value.Date (DateTime(2013,2,1))])
        Lexem.parse "  #21/12/2013#  " |> fst  |> should be (equal [2, Lexem.Value <| Value.Date (DateTime(2013,12,21))])

    [<Test>]
    let ``Parsing values: booleans`` () =
        Lexem.parse "true" |> fst |> should be (equal [0, Lexem.Value <| Value.Bool true])
        Lexem.parse "false" |> fst |> should be (equal [0, Lexem.Value <| Value.Bool false])
        Lexem.parse "  False  FALSE   TRUE   tRuE" |> fst |> should be (equal 
            [
                2, Lexem.Value <| Value.Bool false
                9, Lexem.Value <| Value.Bool false
                17, Lexem.Value <| Value.Bool true
                24, Lexem.Value <| Value.Bool true
            ])

    [<Test>]
    let ``Parsing values: ratings`` () =
        Lexem.parse "[BBB+]" |> fst |> should be (equal [0, Lexem.Value <| Value.Rating (Notch.byName "BBB+")])
        errorAt "[]" |> should be (equal 0)
        errorAt "  [sdf]  [AAA]   [wow!]   [-+-+]" |> should be (equal 2)
        Lexem.parse "[AAA] [C]" |> fst |> should be (equal 
            [0, Lexem.Value <| Value.Rating (Notch.byName "AAA")
             6, Lexem.Value <| Value.Rating (Notch.byName "C")])

    [<Test>]
    let ``Parsing values: strings`` () =
        Lexem.parse "\"hello world!\""|> fst   |> should be (equal [0, Lexem.Value <| Value.String "hello world!"])
        Lexem.parse "\"Hello World!\"" |> fst |> should be (equal [0, Lexem.Value <| Value.String "Hello World!"])
        Lexem.parse "\"Hello    World!\"" |> fst |> should be (equal [0, Lexem.Value <| Value.String "Hello    World!"])
        Lexem.parse "\"hello world!\"     \"hello world!\"            \"Hello    World!\" \"Hello World!\"       " |> fst |> should be (equal 
            [
                0, Lexem.Value <| Value.String "hello world!"
                19, Lexem.Value <| Value.String "hello world!"
                45, Lexem.Value <| Value.String "Hello    World!"
                63, Lexem.Value <| Value.String "Hello World!"
            ])
        Lexem.parse "\"hello world!\"\"hello world!\"" |> fst |> should be (equal 
            [
                0, Lexem.Value <| Value.String "hello world!"
                14, Lexem.Value <| Value.String "hello world!"
            ])
    
    [<Test>]
    let ``Parsing lexems: brackets`` () =
        Lexem.parse "(() ) (  " |> fst |> should be (equal 
            [
                0, Lexem.Delimiter <| Delimiter.OpenBracket
                1, Lexem.Delimiter <| Delimiter.OpenBracket
                2, Lexem.Delimiter <| Delimiter.CloseBracket
                4, Lexem.Delimiter <| Delimiter.CloseBracket
                6, Lexem.Delimiter <| Delimiter.OpenBracket
            ])

    [<Test>]
    let ``Parsing lexems: variables`` () =
        // valid variable names
        Lexem.parse "$a" |> fst |> should be (equal [0, Lexem.Variable <| Variable.Global "A"])
        Lexem.parse "$a.b" |> fst |> should be (equal [0, Lexem.Variable <| Variable.Object ("A", "B")])
        Lexem.parse "$a.b $F $eee_11.a23" |> fst  |> should be (equal 
            [
                0, Lexem.Variable <| Variable.Object ("A", "B")
                5, Lexem.Variable <| Variable.Global "F"
                8, Lexem.Variable <| Variable.Object ("EEE_11", "A23")
            ])
        
        // invalid variable names
        errorAt "$a.b $c $DDDD.312r4wefr3" |> should be (equal 13)
        errorAt "$1" |> should be (equal 0)
        errorAt "$a $_"|> should be (equal 3)

    [<Test>]
    let ``Parsing lexems: function calls`` () =
        Lexem.parse "Hello()" |> fst |> should be (equal 
            [0, Lexem.Function "HELLO"
             5, Lexem.Delimiter Delimiter.OpenBracket
             6, Lexem.Delimiter Delimiter.CloseBracket
            ])
        Lexem.parse "Hello(12)" |> fst |> should be (equal 
            [
                0, Lexem.Function "HELLO"
                5, Lexem.Delimiter Delimiter.OpenBracket
                6, Lexem.Value <| Value.Integer 12L
                8, Lexem.Delimiter Delimiter.CloseBracket
            ])
        Lexem.parse "OhMyGod(12, 23, $a, true)" |> fst |> should be (equal 
            [
                0, Lexem.Function "OHMYGOD"
                7, Lexem.Delimiter Delimiter.OpenBracket
                8, Lexem.Value <| Value.Integer 12L
                10, Lexem.Delimiter <| Delimiter.Comma
                12, Lexem.Value <| Value.Integer 23L
                14, Lexem.Delimiter <| Delimiter.Comma
                16, Lexem.Variable <| Variable.Global "A"
                18, Lexem.Delimiter <| Delimiter.Comma
                20, Lexem.Value <| Value.Bool true
                24, Lexem.Delimiter Delimiter.CloseBracket
            ])
        Lexem.parse "OhMyGod(Hello(12), Bye(23, $alpha.beta_11), $a, false)" |> fst  |> should be (equal 
            [ 0, Lexem.Function "OHMYGOD"
              7, Lexem.Delimiter Delimiter.OpenBracket
              8, Lexem.Function "HELLO"
              13, Lexem.Delimiter Delimiter.OpenBracket
              14, Lexem.Value <| Value.Integer 12L
              16, Lexem.Delimiter Delimiter.CloseBracket
              17, Lexem.Delimiter Delimiter.Comma
              19, Lexem.Function "BYE"
              22, Lexem.Delimiter Delimiter.OpenBracket
              23, Lexem.Value <| Value.Integer 23L
              25, Lexem.Delimiter Delimiter.Comma
              27, Lexem.Variable <| Variable.Object ("ALPHA", "BETA_11")
              41, Lexem.Delimiter Delimiter.CloseBracket
              42, Lexem.Delimiter Delimiter.Comma
              44, Lexem.Variable <| Variable.Global "A"
              46, Lexem.Delimiter Delimiter.Comma
              48, Lexem.Value <| Value.Bool false
              53, Lexem.Delimiter Delimiter.CloseBracket
            ])

    [<Test>]
    let ``Parsing lexems: operations`` () = 
        Lexem.parse "+" |> fst |> should be (equal [0, Lexem.Operation "+"])
        Lexem.parse "-" |> fst |> should be (equal [0, Lexem.Operation "-"])
        Lexem.parse "*" |> fst |> should be (equal [0, Lexem.Operation "*"])
        Lexem.parse "/" |> fst |> should be (equal [0, Lexem.Operation "/"])
        Lexem.parse "and" |> fst |> should be (equal [0, Lexem.Operation "and"])
        Lexem.parse "or" |> fst |> should be (equal [0, Lexem.Operation "or"])
        Lexem.parse "not" |> fst |> should be (equal [0, Lexem.Operation "not"])
        Lexem.parse "=" |> fst |> should be (equal [0, Lexem.Operation "="])
        Lexem.parse "<>" |> fst |> should be (equal [0, Lexem.Operation "<>"])
        Lexem.parse ">" |> fst |> should be (equal [0, Lexem.Operation ">"])
        Lexem.parse "<" |> fst |> should be (equal [0, Lexem.Operation "<"])
        Lexem.parse ">=" |> fst |> should be (equal [0, Lexem.Operation ">="])
        Lexem.parse "<=" |> fst  |> should be (equal [0, Lexem.Operation "<="])

    [<Test>]
    let ``Parsing lexems: all together`` () = 
        Lexem.parse "1+2=4" |> fst |> should be (equal 
            [0, Lexem.Value <| Value.Integer 1L
             1, Lexem.Operation "+"
             2, Lexem.Value <| Value.Integer 2L
             3, Lexem.Operation "="
             4, Lexem.Value <| Value.Integer 4L])

        Lexem.parse "If($a = \"hello\", $x.alpha + 1, 0)" |> fst  |> should be (equal 
            [0, Lexem.Function "IF"
             2, Lexem.Delimiter Delimiter.OpenBracket
             3, Lexem.Variable <| Variable.Global "A"
             6, Lexem.Operation "="
             8, Lexem.Value <| Value.String "hello"
             15, Lexem.Delimiter Delimiter.Comma
             17, Lexem.Variable <| Variable.Object ("X", "ALPHA")
             26, Lexem.Operation "+"
             28, Lexem.Value <| Value.Integer 1L
             29, Lexem.Delimiter Delimiter.Comma
             31, Lexem.Value <| Value.Integer 0L
             32, Lexem.Delimiter Delimiter.CloseBracket])

    [<Test>]
    let ``Grammar`` () =
        let a = "1+2=4" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             0, Syntel.Value <| Value.Integer 1L
             2, Syntel.Value <| Value.Integer 2L
             1, Syntel.Operation "+"
             4, Syntel.Value <| Value.Integer 4L
             3, Syntel.Operation "="
            ])

        let a = "-1+2=4" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             1, Syntel.Value <| Value.Integer 1L
             0, Syntel.Operation "_"
             3, Syntel.Value <| Value.Integer 2L
             2, Syntel.Operation "+"
             5, Syntel.Value <| Value.Integer 4L
             4, Syntel.Operation "="
            ])

        let a = "not true" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             4, Syntel.Value <| Value.Bool true
             0, Syntel.Operation "not"
            ])

        let a = "false and not true" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             0, Syntel.Value <| Value.Bool false
             14, Syntel.Value <| Value.Bool true
             10, Syntel.Operation "not"
             6, Syntel.Operation "and"
            ])


        let x = Syntan.grammarize

        let a = "1+-2=4" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             0, Syntel.Value <| Value.Integer 1L
             3, Syntel.Value <| Value.Integer 2L
             2, Syntel.Operation "_"
             1, Syntel.Operation "+"
             5, Syntel.Value <| Value.Integer 4L
             4, Syntel.Operation "="
            ])

        let a = "(1+2)*3" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             1, Syntel.Value <| Value.Integer 1L
             3, Syntel.Value <| Value.Integer 2L
             2, Syntel.Operation "+"
             6, Syntel.Value <| Value.Integer 3L
             5, Syntel.Operation "*"
            ])

        let a = "1+2*3" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             0, Syntel.Value <| Value.Integer 1L
             2, Syntel.Value <| Value.Integer 2L
             4, Syntel.Value <| Value.Integer 3L
             3, Syntel.Operation "*"
             1, Syntel.Operation "+"
            ])


        let a = "1+2-3" |> Syntan.grammarizeExtended |> Seq.toList
        logger.InfoF "%A" a
        a |> should be (equal 
            [
             0, Syntel.Value <| Value.Integer 1L
             2, Syntel.Value <| Value.Integer 2L
             4, Syntel.Value <| Value.Integer 3L
             3, Syntel.Operation "-"
             1, Syntel.Operation "+"
            ])

        let a = "(1+2)/(3+4)" |> Syntan.grammarizeExtended 
        logger.InfoF "%A" a
        a |> should be (equal [ 1, Syntel.Value <| Value.Integer 1L
                                3, Syntel.Value <| Value.Integer 2L
                                2, Syntel.Operation "+"
                                7, Syntel.Value <| Value.Integer 3L
                                9, Syntel.Value <| Value.Integer 4L
                                8, Syntel.Operation "+"
                                5, Syntel.Operation "/"])

        let a = "(1+2)/((3+4*6)/(12+3*2))" |> Syntan.grammarizeExtended 
        logger.InfoF "%A" a
        a |> should be (equal [ 1, Syntel.Value <| Value.Integer 1L
                                3, Syntel.Value <| Value.Integer 2L
                                2, Syntel.Operation "+"
                                8, Syntel.Value <| Value.Integer 3L
                                10, Syntel.Value <| Value.Integer 4L
                                12, Syntel.Value <| Value.Integer 6L
                                11, Syntel.Operation "*"
                                9, Syntel.Operation "+"
                                16, Syntel.Value <| Value.Integer 12L
                                19, Syntel.Value <| Value.Integer 3L
                                21, Syntel.Value <| Value.Integer 2L
                                20, Syntel.Operation "*"
                                18, Syntel.Operation "+"
                                14, Syntel.Operation "/"
                                5, Syntel.Operation "/"])

        let a = "OhMyGod()" |> Syntan.grammarizeExtended 
        a |> should be (equal [ 0, Syntel.Function "OHMYGOD"])

        let a = "OhMyGod(12)" |> Syntan.grammarizeExtended 
        a |> should be (equal [ 8, Syntel.Value <| Value.Integer 12L
                                0, Syntel.Function "OHMYGOD"])

        let a = "OhMyGod(12, 13)" |> Syntan.grammarizeExtended 
        a |> should be (equal [ 8, Syntel.Value <| Value.Integer 12L
                                12, Syntel.Value <| Value.Integer 13L
                                0, Syntel.Function "OHMYGOD"])
                                
        let a = "OhMyGod(Hello(12), Bye(23, $alpha.beta_11), $a, false)" |> Syntan.grammarizeExtended 
        a |> should be (equal [ 14, Syntel.Value <| Value.Integer 12L
                                8, Syntel.Function "HELLO"
                                23, Syntel.Value <| Value.Integer 23L
                                27, Syntel.Variable <| Variable.Object ("ALPHA", "BETA_11")
                                19, Syntel.Function "BYE"
                                44, Syntel.Variable <| Variable.Global "A"
                                48, Syntel.Value <| Value.Bool false
                                0, Syntel.Function "OHMYGOD"])


    let analyzeAndApply code vars = (code |> Syntan.grammarize, vars |> Map.toDict) |> Interpreter.evaluate      

    [<Test>]
    let ``Interpretation`` () =
        analyzeAndApply "1+2=4" Map.empty |> should be (equal (Value.Bool false))
        analyzeAndApply "2+2=4" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "not true" Map.empty |> should be (equal (Value.Bool false))
        analyzeAndApply "false and not true" Map.empty |> should be (equal (Value.Bool false))
        analyzeAndApply "(1+2)*3" Map.empty |> should be (equal (Value.Integer 9L))
        analyzeAndApply "1+2*3" Map.empty |> should be (equal (Value.Integer 7L))
        analyzeAndApply "1+2-3" Map.empty |> should be (equal (Value.Integer 0L))
        analyzeAndApply "(4+6)/(1+2)" Map.empty |> should be (equal (Value.Integer 3L)) 
        analyzeAndApply "(4+6)/(1+1)" Map.empty |> should be (equal (Value.Integer 5L)) 
        analyzeAndApply "(1+2.0)/(3+4)" Map.empty |> should be (equal (Value.Double ((double 1+2.0)/double(3+4)))) 

        analyzeAndApply "IIf($a, 1, 2)" Map.empty |> should be (equal (Value.Nothing)) 
        analyzeAndApply "IIf(true, 1, 2)" Map.empty |> should be (equal (Value.Integer 1L)) 
        analyzeAndApply "IIf(false, 1, 2)" Map.empty |> should be (equal (Value.Integer 2L)) 
        analyzeAndApply "$a + 1" Map.empty |> should be (equal Value.Nothing)
        analyzeAndApply "[AA+] + 1= [AAA]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[AAA] > [AAA]" Map.empty |> should be (equal (Value.Bool false))
        analyzeAndApply "[AAA] > [AA-]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[AAA] >= [AA-]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[BBB] >= [BBB]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[BBB]+1 > [BBB]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[BBB]-1 <= [BBB]" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[BBB]-1 = [BBB]-1" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "[BBB]-1 <> [BBB]-2" Map.empty |> should be (equal (Value.Bool true))
        analyzeAndApply "true = True" Map.empty |> should be (equal (Value.Bool true))

        ["A", box 2] |> Map.ofList |> analyzeAndApply "$a + 1" |> should be (equal (Value.Integer 3L)) 

    [<Test>]
    let ``Complex examples`` () =
    
        let doExpression expr res =
            [("InstrumentName", box "Name")
             ("$Coupon", box 5)] 
            |> Map.ofList 
            |> analyzeAndApply expr 
            |> should be (equal (Value.String res))

        let expression = "$InstrumentName + IIf(Not IsNothing($Maturity), \" '\" + Format(\"{0:MMM-yy}\", $Maturity), \"\")"
        let grammar = expression |> Syntan.grammarizeExtended
        logger.InfoF "Grammar is %A" grammar
        
        grammar |> should be (equal 
            [(0, Variable (Global "INSTRUMENTNAME"))
             (36, Variable (Global "MATURITY"))
             (26, Function "ISNOTHING")
             (22, Operation "not")
             (48, Value (String " '"))
             (62, Value (String "{0:MMM-yy}"))
             (76, Variable (Global "MATURITY"))
             (55, Function "FORMAT")
             (53, Operation "+")
             (88, Value (String ""))
             (18, Function "IIF")
             (16, Operation "+")
            ])

        doExpression expression ""

        let expression = "$InstrumentName + IIf(Not IsNothing($Coupon), \" \" + Format(\"{0:0.00}\", $Coupon), \"\")"
        let grammar = expression |> Syntan.grammarizeExtended
        logger.InfoF "Grammar is %A" grammar

        grammar |> should be (equal 
            [(0, Variable (Global "INSTRUMENTNAME"))
             (36, Variable (Global "COUPON"))
             (26, Function "ISNOTHING")
             (22, Operation "not")
             (46, Value (String " "))
             (59, Value (String "{0:0.00}"))
             (71, Variable (Global "COUPON"))
             (52, Function "FORMAT")
             (50, Operation "+")
             (81, Value (String ""))
             (18, Function "IIF")
             (16, Operation "+")
            ])

        doExpression expression ""


        let expression = "$InstrumentName + IIf(Not IsNothing($Coupon), \" \" + Format(\"{0:0.00}\", $Coupon), \"\") + IIf(Not IsNothing($Maturity), \" '\" + Format(\"{0:MMM-yy}\", $Maturity))"
        
        let grammar = expression |> Syntan.grammarizeExtended
        logger.InfoF "Grammar is %A" grammar

        grammar |> should be (equal 
            [(0, Variable (Global "INSTRUMENTNAME"))
             (36, Variable (Global "COUPON"))
             (26, Function "ISNOTHING")
             (22, Operation "not")
             (46, Value (String " "))
             (59, Value (String "{0:0.00}"))
             (71, Variable (Global "COUPON"))
             (52, Function "FORMAT")
             (50, Operation "+")
             (81, Value (String ""))
             (18, Function "IIF")
             (105, Variable (Global "MATURITY"))
             (95, Function "ISNOTHING")
             (91, Operation "not")
             (117, Value (String " '"))
             (131, Value (String "{0:MMM-yy}"))
             (145, Variable (Global "MATURITY"))
             (124, Function "FORMAT")
             (122, Operation "+")
             (87, Function "IIF")
             (85, Operation "+")
             (16, Operation "+")
            ])


        doExpression expression ""
