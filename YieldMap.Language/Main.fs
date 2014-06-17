namespace YieldMap.Language

open System
open System.Text
open System.Text.RegularExpressions
open YieldMap.Tools.Logging

type GrammarError = { str : string; message : string; position : int }

exception GrammarException of GrammarError
    with override x.ToString () = 
            let pointingString pos = StringBuilder().Append('-', pos).Append('^').ToString()

            let data = x.Data0
            let pos = data.position
            sprintf "Error at position %d: %s\n%s\n%s" pos data.message data.str (pointingString pos)
   
module Lexan =
    let logger = LogFactory.create "Language.Lexan"

    module internal Helper = 
        let trimStart (str : string) = 
            if String.IsNullOrEmpty str then (str, 0)
            else
                let mutable i = 0
                while str.[i] = ' ' do i <- i + 1
                (str.Substring i, i)


        let extractUntilBracketClose (str : string) =
            let rec extract (buffer : System.Text.StringBuilder) (str : string) level = 
                if String.IsNullOrEmpty str then
                    None
                else
                    let ch = str.[0]
                    let rest = str.Substring 1
                    if ch = ')' && level = 0 then Some buffer
                    else
                        extract (buffer.Append ch) rest <|
                            if ch = ')' then level-1
                            elif ch = '(' then level + 1
                            else level

            extract (System.Text.StringBuilder()) str 0
            |> Option.map (fun s -> s.ToString())
            

    exception internal AnalyzerException of string

    type Value = 
    | Date of DateTime
    | Rating of string
    | String of string
    | Bool of bool
    | Integer of int64
    | Double of double
    with
        override x.ToString () = 
            match x with
            | Date dt -> sprintf "Date(%s)" (dt.ToString("dd/MM/yyyy"))
            | Rating str -> sprintf "Rating(%s)" str
            | String str -> sprintf "String(%s)" str
            | Bool b -> sprintf "Bool(%s)" (b.ToString())
            | Integer i -> sprintf "Integer(%d)" i
            | Double d -> sprintf "Double(%f)" d
        static member internal extract (str : string) = 
            let ch = str.[0]
            let lStr = str.ToLower()

            if ch = '#' then // date
                let m = Regex.Match(str, "^#(?<dd>\d{1,2})/(?<mm>\d{1,2})/(?<yy>\d{4})#")
                if m.Success then
                    let dd = Int32.Parse <| m.Groups.Item("dd").Captures.Item(0).Value
                    let mm = Int32.Parse <| m.Groups.Item("mm").Captures.Item(0).Value
                    let yy = Int32.Parse <| m.Groups.Item("yy").Captures.Item(0).Value
                    (Date <| DateTime(yy, mm, dd), m.Length)
                else raise <| AnalyzerException ("Invalid date")
            elif ch = '[' then // rating
                let m = Regex.Match(str, "\[(?<rating>[^\]]*)\]")
                if m.Success then
                    let rating = m.Groups.Item("rating").Captures.Item(0).Value
                    (Rating rating, m.Length)
                else raise <| AnalyzerException ("Invalid rating")
            elif ch = '"' then // string
                let m = Regex.Match(str, "^\"(?<str>[^\"]*)\"")
                if m.Success then
                    let str = m.Groups.Item("str").Captures.Item(0).Value
                    (String str, m.Length)
                else raise <| AnalyzerException ("Invalid string")
            elif lStr.StartsWith("true") then // bool
                (Bool true, 4)
            elif lStr.StartsWith("false") then // bool
                (Bool false, 5)
            else // number
                let m = Regex.Match(str, "^(?<num>\d+\.\d+|-?\d+)")
                if m.Success then
                    let str = m.Groups.Item("num").Captures.Item(0).Value
                    if str.IndexOf('.') > 0 then
                        let (success, num) = Double.TryParse(str)
                        if success then (Double num, m.Length)
                        else raise <| AnalyzerException ("Invalid float")                
                    else
                        let (success, num) = Int64.TryParse(str)
                        if success then (Value.Integer num, m.Length)
                        else raise <| AnalyzerException ("Invalid integer")                        
                else raise <| AnalyzerException ("Invalid number")                

    type Variable = 
    | Object of string * string
    | Global of string
    with
        override x.ToString () = 
            match x with
            | Object (o,p) -> sprintf "Object %s.%s" o p
            | Global g ->  sprintf "Global %s" g

    module Operations =
        let private operations = [|"+"; "-"; "*"; "/"; "and"; "not"; "or"; "="; "<>"; ">="; "<="; ">"; "<"|]
        
        let tryExtract (str : string) = 
            let str = str.ToLower()
            operations |> Array.tryFind str.StartsWith

    type Delimiter = 
    | OpenBracket 
    | CloseBracket
    | Comma
    with 
        override x.ToString () = 
            match x with
            | OpenBracket -> "("
            | CloseBracket -> ")"
            | Comma -> ","

    type LexemPos = int * Lexem
    and Lexem = 
    | Delimiter of Delimiter
    | Value of Value
    | Variable of Variable
    | Operation of string
    | Function of string
    with 
        override x.ToString () = 
            match x with
            | Delimiter d -> sprintf "Delim(%s)" (d.ToString())
            | Value v -> sprintf "Value(%s)" (v.ToString())
            | Variable v -> sprintf "Variable(%s)" (v.ToString())
            | Operation o -> sprintf "Operation(%s)" (o.ToString())
            | Function f -> sprintf "Function(%s)" (f.ToString())

        static member private extact (str : string) basis = 
            logger.TraceF "extract %s" str
            let ch = str.[0]
            if ch = '$' then 
                let varname = "(?:_[a-zA-Z0-9_]+|[a-zA-Z][a-zA-Z0-9_]*)"
                let varrx = String.Format("^\$(?<objname>{0})\.(?<fieldname>{0})|^\$(?<objname>{0})", varname)
                let m = Regex.Match(str, varrx)
                if m.Success then
                    let globalName = m.Groups.Item("objname").Captures.Item(0).Value.ToUpper()
                    let propertyGroup = m.Groups.Item("fieldname")
                    let var = 
                        if propertyGroup.Success then
                            let propertyName = propertyGroup.Captures.Item(0).Value.ToUpper()
                            Lexem.Variable <| Variable.Object (globalName, propertyName)
                        else Lexem.Variable <| Variable.Global globalName
                    (var, m.Length)
                else raise <| AnalyzerException ("Failed to parse variable name")

            elif ch = ',' then (Lexem.Delimiter <| Delimiter.Comma, 1)
            elif ch = '(' then (Lexem.Delimiter <| Delimiter.OpenBracket, 1)
            elif ch = ')' then (Lexem.Delimiter <| Delimiter.CloseBracket, 1)

            elif Regex.IsMatch (str, "^[a-zA-Z]\w+\(.*\)") then
                let m = Regex.Match(str, "^(?<name>[a-zA-Z]\w+)\((?<params>.*)")
                if m.Success then
                    let str = str.Substring m.Length                    
                    let func = m.Groups.Item("name").Captures.Item(0).Value.ToUpper()
                    let prms = 
                        m.Groups.Item("params").Captures.Item(0).Value 
                        |> Helper.extractUntilBracketClose
                    match prms with
                    | Some parameters -> 
                        let call = Lexem.Function func//{ name = func; parameters = Lexem.parseInternal parameters (basis + func.Length + 1) }
                        (call, func.Length) // + 1 + parameters.Length + 1
                    | None -> raise <| AnalyzerException ("Failed to parse function: brackets unbalanced")
                else raise <| AnalyzerException ("Failed to parse function name")

             else 
                match Operations.tryExtract str with
                | Some op -> 
                    (Lexem.Operation op, op.Length)
                | None ->
                    let (value, length) = Value.extract str
                    (Lexem.Value value, length)

        static member private parseInternal (s : string) basis = 
            logger.TraceF "Parsing [%s]" s

            let rec parseRecursive str offset stack  = 
                if String.IsNullOrWhiteSpace str then 
                    stack
                else
                    //=========================================================== 
                    // String structure and all offsets
                    //===========================================================
                    //    some call                        current lexem
                    //   FunctionCall( *parsed*   whitespace  3123214
                    // --------------|----------|------------|-------|-
                    //         basis-^   offset-^     spaces-^   len-^
                    //=========================================================== 
                    let (str, spaces) = Helper.trimStart str
                    let local = offset + spaces                     

                    try
                        let lex, len = Lexem.extact str (basis + local)
                        (basis + local, lex) :: stack |>
                            if String.length str > len then
                                parseRecursive (str.Substring len) (local + len)
                            else id
                    with :? AnalyzerException as ae ->
                        let ex = GrammarException { str = s; message = ae.Data0; position = local }
                        logger.WarnEx "Problem!" ex
                        raise ex
                        
            let res = parseRecursive (s.TrimEnd()) 0 []
            res |> List.rev

        static member parse (s : string) = Lexem.parseInternal s 0, s

module Syntan = 
    open Lexan
    open YieldMap.Tools.Aux
    open FSharpx.Collections

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

    type Op = 
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
                if o = "not" then 8
                elif o |- set ["and"; "or"] then 7
                elif o |- set ["*"; "/"] then 6
                elif o |- set ["+"; "-"] then 5 // TODO UNARY ELEVATION
                elif o |- set ["="; "<>"; ">="; "<="; ">"; "<"] then 4
                else raise <| SyntaxException "Unknown operation"
            | _ -> raise <| SyntaxException "Unexpected token"
    
    let grammar lexems source = 
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

        let d, o = // TODO UNARY ELEVATION
            ((List.empty, List.empty), lexems) 
            ||> List.fold (fun s lex -> 
                let (output, operators) = s
                match lex with
                | (pos, Lexem.Delimiter d) -> 
                    match d with
                    | Delimiter.OpenBracket ->
                        (output, Op.Bracket :: operators) // to stack
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
                        (popped |> List.map Op.toSyntel) @ output, rest 

                    | Delimiter.Comma ->
                        // 1) pop stack until openbracket
                        let popped, rest, found = popToBracket1 operators

                        // 2) no openbracket => imbalance
                        if not found then raise <| GrammarException { str = source; message = "Brackets imbalance"; position = pos }

                        // 3) Add all popped elements to output
                        (popped |> List.map Op.toSyntel) @ output, rest 

                | (pos, Lexem.Function f) -> // DONE
                    output, (Op.Function (pos, f)) :: operators

                | (pos, oper) & (_, Lexem.Operation o) ->
                    // 1) pop stack until priority greater
                    let newPriority = Lexem.priority oper
                    let newOp = Op.Operation (pos, o, newPriority)
                    let popped, rest = pushAway newPriority operators
                    // 2) push current operation to stack
                    (popped |> List.map Op.toSyntel) @ output, newOp :: rest 

                | (p, some) -> // value or variable
                    let syntem = 
                        match some with
                        | Lexem.Value v -> Syntel.Value v
                        | Lexem.Variable v -> Syntel.Variable v
                        | _ -> raise <| SyntaxException (sprintf "Unexpected lexem : %A" some)
                    (p, syntem) :: output, operators // var or val priority is zero
            ) 
        try
            let popped, rest, found = o |> popToBracket
            if found && not <| List.isEmpty rest then raise <| GrammarException { str = source; message = "Brackets imbalance"; position = 0 }
            (popped |> List.map Op.toSyntel) @ d |> List.rev
        with :? SyntaxException as ae ->
            let ex = GrammarException { str = source; message = ae.Data0; position = 0 }
            logger.WarnEx "Problem!" ex
            raise ex