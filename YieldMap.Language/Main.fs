namespace YieldMap.Language

open System
open System.Text
open System.Text.RegularExpressions
open YieldMap.Tools.Logging
   
module Lexan =
    let logger = LogFactory.create "Language.Lexan"

    module internal Helper = 
        let trimStart (str : string) = 
            if String.IsNullOrEmpty str then (str, 0)
            else
                let mutable i = 0
                while str.[i] = ' ' do i <- i + 1
                (str.Substring i, i)

        let pointingString pos = StringBuilder().Append('-', pos).Append('^').ToString()

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

    type LexicalError = { str : string; message : string; position : int }

    exception LexicalException of LexicalError
        with override x.ToString () = 
                 let data = x.Data0
                 let pos = data.position
                 sprintf "Error at position %d: %s\n%s\n%s" pos data.message data.str (Helper.pointingString pos)

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

    type FunctionCall = 
        {
            name : string
            parameters : LexemPos list
        }
        with override x.ToString () = sprintf "Name %s, Params %A" x.name x.parameters
    
    and LexemPos = int * Lexem

    and Lexem = 
    | Delimiter of Delimiter
    | Value of Value
    | Variable of Variable
    | Operation of string
    | FunctionCall of FunctionCall
    with 
        override x.ToString () = 
            match x with
            | Delimiter d -> sprintf "Delim(%s)" (d.ToString())
            | Value v -> sprintf "Value(%s)" (v.ToString())
            | Variable v -> sprintf "Variable(%s)" (v.ToString())
            | Operation o -> sprintf "Operation(%s)" (o.ToString())
            | FunctionCall f -> sprintf "FunctionCall(%s)" (f.ToString())

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
                        let call = Lexem.FunctionCall { name = func; parameters = Lexem.parseInternal parameters (basis + func.Length + 1) }
                        (call, func.Length + 1 + parameters.Length + 1)
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
                        let ex = LexicalException { str = s; message = ae.Data0; position = local }
                        logger.WarnEx "Problem!" ex
                        raise ex
                        
            let res = parseRecursive (s.TrimEnd()) 0 []
            res |> List.rev

        static member parse (s : string) = Lexem.parseInternal s 0

module SinkingStack = 
    ()

module Syntan = 
    open Lexan
    open YieldMap.Tools.Aux

    let logger = LogFactory.create "Language.Syntan"

    exception SyntaxException of string

//    type SynFunctionCall = 
//        {
//            name : string
//            parameters : SyntemPos list
//        }
//        with override x.ToString () = sprintf "Name %s, Params %A" x.name x.parameters
//    and SyntemPos = int * Syntem
//    and Syntem = 
//    | Value of Value
//    | Variable of Variable
//    | Operation of string
//    | FunctionCall of SynFunctionCall

    type State = 
    | Expression
    | Parameters

    let levelPriority = 10

    let basicPriority = function
        | Lexem.FunctionCall _ -> 1
        | Lexem.Operation o -> 
            if o |- set ["*"; "/"] then 2
            elif o |- set ["+"; "-"] then 3
            elif o |- set ["="; "<>"; ">="; "<="; ">"; "<"] then 4
            elif o |- set [ "and"; "not"; "or"] then 5
            else failwith "Unknown operation"
        | Lexem.Value _ -> 6
        | Lexem.Variable _ -> 6
        | _ -> failwith "Unexpected token"

    let prioritize lexems = 
        let initalState = (0, 0, []) // bracket level * maxLevel * (LexemPos * priority) list
        let lastLevel, maxLevel, prioritized = 
            lexems |> List.fold (fun (l, m, prioritizedItems) lex -> 
                match lex with
                | (_, Lexem.Delimiter d) ->
                    let level, max = 
                        match d with
                        | Delimiter.OpenBracket -> l+1, l+1
                        | Delimiter.CloseBracket -> l-1, l
                        | Delimiter.Comma -> l, l
                    (level, max, prioritizedItems)
                | (p, some) -> 
                    let basic = basicPriority some
                    (l, m, (p, some, levelPriority * l + basic) :: prioritizedItems))
                initalState
        if lastLevel <> 0 then failwith "Brackets unbalanced"
        prioritized |> List.rev |> List.map (fun (position, lexem, priority) -> 
            (position, lexem, levelPriority * (maxLevel + 1) - priority)
        )
   
    let syntax lexems =
        let rec analyze lexems state level acc =
            match lexems with 
            | lex :: rest -> acc
            | [] -> acc
        analyze lexems State.Expression 0 []