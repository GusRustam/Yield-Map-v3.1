namespace YieldMap.Language

open System
open System.Text
open System.Text.RegularExpressions
open YieldMap.Tools.Logging
   
module Analyzer =
    let logger = LogFactory.create "UnitTests.Language"

    module internal Helper = 
        let trimStart (str : string) = 
            if String.IsNullOrEmpty str then (str, 0)
            else
                let mutable i = 0
                while str.[i] = ' ' || str.[i] = ',' do i <- i + 1
                (str.Substring i, i)

        let pointingString pos = StringBuilder().Append('-', pos).Append('^').ToString()

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

    type FunctionCall = 
        {
            name : string
            parameters : LexemPos list
        }
        with override x.ToString () = sprintf "Name %s, Params %A" x.name x.parameters
    
    and LexemPos = int * Lexem

    and Lexem = 
    | OpenBracket 
    | CloseBracket
    | Value of Value
    | Variable of Variable
    | Operation of string
    | FunctionCall of FunctionCall
    with 
        override x.ToString () = 
            match x with
            | OpenBracket -> "("
            | CloseBracket -> ")"
            | Value v -> sprintf "Value(%s)" (v.ToString())
            | Variable v -> sprintf "Variable(%s)" (v.ToString())
            | Operation o -> sprintf "Operation(%s)" (o.ToString())
            | FunctionCall f -> sprintf "FunctionCall(%s)" (f.ToString())

        static member private extact (str : string) = 
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

            elif ch = '(' then (Lexem.OpenBracket, 1)
            elif ch = ')' then (Lexem.CloseBracket, 1)

            elif Regex.IsMatch (str, "^[a-zA-Z]\w+\(.*\)") then
                let m = Regex.Match(str, "^(?<name>[a-zA-Z]\w+)\((?<params>.*)\)")
                if m.Success then
                    let str = str.Substring m.Length                    
                    let func = m.Groups.Item("name").Captures.Item(0).Value.ToUpper()
                    let prms = m.Groups.Item("params").Captures.Item(0).Value
                    let call = Lexem.FunctionCall { name = func; parameters = Lexem.parse prms }
                    (call, m.Length)
                else raise <| AnalyzerException ("Failed to parse function name")

             else // TODO OPERATIONS!!!
                let (value, length) = Value.extract str
                (Lexem.Value value, length)

        static member parse (s : string) = 
            logger.TraceF "Parsing [%s]" s

            let rec doParse str positionInString stack  = 
                if String.IsNullOrWhiteSpace str then 
                    stack
                else
                    let (str, shift) = Helper.trimStart str
                    let offset = positionInString + shift

                    try
                        let lex, len = Lexem.extact str
                        (offset, lex) :: stack |>
                            if String.length str > len then
                                doParse (str.Substring len) (offset + len)
                            else id
                    with :? AnalyzerException as ae ->
                        let ex = LexicalException { str = s; message = ae.Data0; position = offset }
                        logger.WarnEx "Problem!" ex
                        raise ex
                        
            let res = doParse (s.TrimEnd()) 0 []
            res |> List.rev