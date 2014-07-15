namespace YieldMap.Language

open System
open System.Text
open System.Text.RegularExpressions

open YieldMap.Tools.Logging
open YieldMap.Tools.Aux

open Lexan
open Exceptions

module Syntan = 
    let logger = LogFactory.create "Language.Syntan"
    exception internal SyntaxException of string

    type Syntel = 
    | Value of Value
    | Variable of Variable
    | Operation of string
    | Function of string
    with 
        override x.ToString () = 
            match x with
            | Value v -> sprintf "Value(%s)" (v.ToString())
            | Variable v -> sprintf "Variable(%s)" (v.ToString())
            | Operation o -> sprintf "Operation(%s)" (o.ToString())
            | Function f -> sprintf "Function(%s)" (f.ToString())

    type internal Op = 
    | Bracket
    | Function of int * string 
    | Operation of int * string * int
    with
        override x.ToString () = 
            match x with
            | Bracket  -> "Bracket"
            | Operation (pos, name, priority) -> sprintf "Operation(%d, %s, %d)" pos name priority
            | Function (pos, name) -> sprintf "Function(%d, %s)" pos name
            
        static member isBracket op = match op with Op.Bracket -> true | _ -> false
        static member isFunction op = match op with Op.Function (_, _) -> true | _ -> false
        static member isOperation op = match op with Op.Operation (_, _, _) -> true | _ -> false
        static member toSyntel op = 
            match op with 
            | Op.Operation (pos, name, _) -> pos, Syntel.Operation (name) 
            | Op.Function (pos, name) -> pos, Syntel.Function name 
            | _ -> raise <| SyntaxException (sprintf "%A is not operation or function" op)

    type Lexem with  
        static member priority lex = 
            match lex with
            | Lexem.Function _ -> 9
            | Lexem.Operation o -> 
                if o |- set ["not"; "_"] then 8
                elif o |- set ["and"; "or"] then 7
                elif o |- set ["*"; "/"] then 6
                elif o |- set ["+"; "-"] then 5 
                elif o |- set ["="; "<>"; ">="; "<="; ">"; "<"] then 4
                else raise <| SyntaxException "Unknown operation"
            | _ -> raise <| SyntaxException "Unexpected token"

    // Grammar is 
    // <expression> ::= <term> | (<term>)
    // <term> ::= <item> | <item> <op> <expression> | <unary-op> <expression>
    // <item> ::= <value> | <variable> | <function-call> | EMPTY
    // <function-call> = <name> (<params>)
    // <params> ::= <expression> | <expression>, <params>
    // <value> ::= <int> | <float> | <string> | <rating> | <date>
    // <variable> ::= $<name> | $<name>.<name>
    let internal grammar lexems source = 
        let popToBracket operators = 
            let popped, others, found = 
                ((List.empty, List.empty, false), operators)
                ||> List.fold (fun (l, r, f) o ->
                    if f then (l, o::r, f)
                    elif Op.isBracket o then (l, r, true)
                    else (o::l, r, f))
            popped, others |> List.rev, found

        let popToBracket1 operators = 
            let popped, others, found = 
                ((List.empty, List.empty, false), operators)
                ||> List.fold (fun (l, r, f) o ->
                    if f then (l, o::r, f)
                    elif Op.isBracket o then (l, o::r, true) // here's o::r instead of just r
                    else (o::l, r, f))
            popped, others |> List.rev, found
            
        let pushAway newPriority operators =
            let popped, others, _ = 
                ((List.empty, List.empty, false), operators)
                ||> List.fold (fun (l, r, f) o ->
                    if f then (l, o::r, f)
                    else match o with
                         | Op.Operation (pos,name,priority) -> 
                         if priority > newPriority then (o::l, r, f)
                         else (l, o::r, true)
                         | _ -> (l, o::r, true))
            popped, others |> List.rev

        let d, o, _ = 
            ((List.empty, List.empty, None), lexems) 
            ||> List.fold (fun s lex -> 
                let (output, operators, prev) = s
                match lex with
                | (pos, Lexem.Delimiter d) & (_, previous) -> 
                    match d with
                    | Delimiter.OpenBracket ->
                        output, Op.Bracket :: operators, Some previous // to stack
                    | Delimiter.CloseBracket ->
                        // 1) pop until openbracket
                        // 2) remove openbracket and look at what left
                        let popped, rest, found = popToBracket operators

                        // 4) no openbracket => imbalance
                        if not found then raise <| GrammarException { str = source; message = "Brackets imbalance"; position = pos }

                        // 3) if function call -> pop too
                        let popped, rest =
                            match rest  with
                            | head :: tail when Op.isFunction head -> (head :: popped), tail
                            | _ -> popped, rest

                        // 5) Add all popped elements to output
                        (popped |> List.map Op.toSyntel) @ output, rest, Some previous

                    | Delimiter.Comma ->
                        // 1) pop stack until openbracket
                        let popped, rest, found = popToBracket1 operators

                        // 2) no openbracket => imbalance
                        if not found then raise <| GrammarException { str = source; message = "Brackets imbalance"; position = pos }

                        // 3) Add all popped elements to output
                        (popped |> List.map Op.toSyntel) @ output, rest, Some previous

                | (pos, Lexem.Function f) & (_, previous) -> // DONE
                    output, (Op.Function (pos, f)) :: operators, Some previous

                | (pos, Lexem.Operation o) & (_, previous) ->
                    // if operation is "-" and previous lexem is None or another Operation then it is unary minus
                    let o = if o = "-" then
                                match prev with
                                | Some l when Lexem.isOperation l -> "_"
                                | None -> "_"
                                | _ -> o
                            else o
                    let oper = Lexem.Operation o

                    // 1) pop stack until priority greater
                    let newPriority = Lexem.priority oper
                    let newOp = Op.Operation (pos, o, newPriority)
                    let popped, rest = pushAway newPriority operators
                    // 2) push current operation to stack
                    (popped |> List.map Op.toSyntel) @ output, newOp :: rest, Some previous

                | (p, some) -> // value or variable
                    let syntem = 
                        match some with
                        | Lexem.Value v -> Syntel.Value v 
                        | Lexem.Variable v -> Syntel.Variable v
                        | _ -> raise <| SyntaxException (sprintf "Unexpected lexem : %A" some)
                    (p, syntem) :: output, operators, Some some // var or val priority is zero
                )
        try
            let popped, rest, found = o |> popToBracket
            if found && not <| List.isEmpty rest then raise <| GrammarException { str = source; message = "Brackets imbalance"; position = 0 }
            (popped |> List.map Op.toSyntel) @ d |> List.rev
        with :? SyntaxException as ae ->
            let ex = GrammarException { str = source; message = ae.Data0; position = 0 }
            logger.WarnEx "Problem!" ex
            raise ex

    let grammarizeExtended str = str |> Lexem.parse ||> grammar |> Seq.ofList
    let grammarize s = s |> grammarizeExtended |> Seq.map snd