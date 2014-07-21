namespace YieldMap.Language

open System
open System.Text
open System.Text.RegularExpressions

open YieldMap.Tools.Logging
open YieldMap.Tools.Ratings
open YieldMap.Tools.Response

open Exceptions

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
    | Rating of Notch
    | String of string
    | Bool of bool
    | Integer of int64
    | Double of double
    | Nothing
    with
        override x.ToString () = 
            match x with
            | Date dt -> String.Format("#{0:dd/mm/yyyy}#", dt)
            | Rating n -> sprintf "Rating(%A)" n.name
            | String str -> str
            | Bool b -> b.ToString()
            | Integer i -> sprintf "%d" i
            | Double d -> sprintf "%f" d
            | Nothing -> "NULL"
        
        static member boxify v =
            match v with
            | Date dt -> box dt
            | Rating n -> null // todo
            | String str -> box str
            | Bool b -> box b
            | Integer i -> box i
            | Double d -> box d
            | Nothing -> null

        member v.asString = 
            match v with
            | Date dt -> dt.ToString("dd/MM/yyyy")
            | Rating n -> sprintf "[%s]" n.name
            | String str -> sprintf "%s" str
            | Bool b -> sprintf "%s" (b.ToString())
            | Integer i -> sprintf "%d" i
            | Double d -> sprintf "%f" d
            | Nothing -> ""

        static member interpret (o : obj) =
            match o with
            | :? Int16 as i -> Integer (int64 i)
            | :? Int32 as i -> Integer (int64 i)
            | :? Int64 as i -> Integer i
            | :? double as d -> Double d
            | :? bool as b -> Bool b
            | :? DateTime as d -> Date d
            | null -> Nothing
            | _ -> 
                let str = o.ToString()
                let m = Regex.Match(str, "\[(?<rating>[^\]]*)\]")
                if m.Success then
                    let rating = m.Groups.Item("rating").Captures.Item(0).Value
                    match rating |> Notch.parse with
                    | Answer notch -> Rating notch
                    | Failure f -> failwith <| sprintf "Error in rating: %A" (f.ToString())
                else
                    String str

        static member internal extract (str : string) = 
            let ch = str.[0]
            let lStr = str.ToLower()

            if ch = '#' then // date
                let m = Regex.Match(str, "^#(?<dd>\d{1,2})/(?<mm>\d{1,2})/(?<yy>\d{4})#")
                if m.Success then
                    let dd = Int32.Parse <| m.Groups.Item("dd").Captures.Item(0).Value
                    let mm = Int32.Parse <| m.Groups.Item("mm").Captures.Item(0).Value
                    let yy = Int32.Parse <| m.Groups.Item("yy").Captures.Item(0).Value
                    Answer (Date <| DateTime(yy, mm, dd), m.Length)
                else Failure (Problem "Invalid date")
            elif ch = '[' then // rating
                let m = Regex.Match(str, "\[(?<rating>[^\]]*)\]")
                if m.Success then
                    let rating = m.Groups.Item("rating").Captures.Item(0).Value
                    match rating |> Notch.parse with
                    | Answer notch -> Answer (Rating notch, m.Length)
                    | Failure f -> Failure f
                else Failure (Problem "Invalid rating")
            elif ch = '"' then // string
                let m = Regex.Match(str, "^\"(?<str>[^\"]*)\"")
                if m.Success then
                    let str = m.Groups.Item("str").Captures.Item(0).Value
                    Answer (String str, m.Length)
                else Failure (Problem "Invalid string")
            elif lStr.StartsWith("true") then // bool
                Answer (Bool true, 4)
            elif lStr.StartsWith("false") then // bool
                Answer (Bool false, 5)
            else // number
                let m = Regex.Match(str, "^(?<num>\d+\.\d+|-?\d+)")
                if m.Success then
                    let str = m.Groups.Item("num").Captures.Item(0).Value
                    if str.IndexOf('.') > 0 then
                        let (success, num) = Double.TryParse(str)
                        if success then 
                            Answer (Double num, m.Length)
                        else Failure (Problem "Invalid float")
                    else
                        let (success, num) = Int64.TryParse(str)
                        if success then 
                            Answer (Value.Integer num, m.Length)
                        else Failure (Problem "Invalid integer")
                else Failure (Problem "Invalid number")

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

    type Delimiter = OpenBracket | CloseBracket | Comma
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

        static member isOperation x = match x with Operation _ -> true | _ -> false

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
                        let call = Lexem.Function func
                        (call, func.Length)
                    | None -> raise <| AnalyzerException ("Failed to parse function: brackets unbalanced")
                else raise <| AnalyzerException ("Failed to parse function name")

             else 
                match Operations.tryExtract str with
                | Some op -> 
                    (Lexem.Operation op, op.Length)
                | None ->
                    match Value.extract str with
                    | Answer (value, length) -> (Lexem.Value value, length)
                    | Failure f -> raise <| AnalyzerException (sprintf "Failed to parse value: %s" (f.ToString()))

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