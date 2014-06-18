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
    | Nothing
    with
        override x.ToString () = 
            match x with
            | Date dt -> sprintf "Date(%s)" (dt.ToString("dd/MM/yyyy"))
            | Rating str -> sprintf "Rating(%s)" str
            | String str -> sprintf "String(%s)" str
            | Bool b -> sprintf "Bool(%s)" (b.ToString())
            | Integer i -> sprintf "Integer(%d)" i
            | Double d -> sprintf "Double(%f)" d
            | Nothing -> "Nothing"
        
        static member interpret (o : obj) =
            match o with
            | :? Int16 as i -> Integer (int64 i)
            | :? Int32 as i -> Integer (int64 i)
            | :? Int64 as i -> Integer i
            | :? double as d -> Double d
            | :? bool as b -> Bool b
            | :? DateTime as d -> Date d
            | _ -> 
                let str = o.ToString()
                let m = Regex.Match(str, "\[(?<rating>[^\]]*)\]")
                if m.Success then
                    let rating = m.Groups.Item("rating").Captures.Item(0).Value
                    Rating rating
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

#nowarn "62"
module Interpreter = 
    open Lexan
    open Syntan
    open YieldMap.Tools.Aux

    // LAZY FUNCTIONS POSSIBLE ONLY IF I FIRST CREATE A TREE FROM A STACK
    // FUNCTIONS WITH VARIABLE NUMBER OF ARGS (OR DIFFERING NUMBER OF ARGS)
    //   ARE NOT IMPLEMENTABLE UNLESS I ADD SOME CALL DELIMITER TO SYNTAX PARSER OUTPUT

    // TODO RATINGS!!! RATINGS ARE COMPARABLE AND ONE CAN USE +/- OPS WITH THEM!

    module private Operations = 
        let applyMath op v1 v2 =
            if op = "+" then
                match v1, v2 with
                | Value.Integer i1, Value.Integer i2 -> Value.Integer (i1 + i2)
                | Value.Double d1, Value.Double d2 -> Value.Double (d1 + d2)
                | Value.Integer i, Value.Double d | Value.Double d, Value.Integer i -> Value.Double (double i + d)
                | Value.String s1, Value.String s2 -> Value.String (s1 + s2)
                | _, Value.Nothing | Value.Nothing, _ -> Value.Nothing
                | _ -> failwith <| sprintf "Operation + is not applicable to args %A %A" v1 v2
            else 
                match v1, v2 with
                | Value.Integer n, Value.Integer m -> 
                    Value.Integer (if op = "*" then n*m elif op = "/" then n/m else n-m)
                | Value.Double n, Value.Double m -> 
                    Value.Double (if op = "*" then n*m elif op = "/" then n/m else n-m)
                | Value.Integer i, Value.Double d ->
                    let n, m = double i, d
                    Value.Double (if op = "*" then n*m elif op = "/" then n/m else n-m)
                | Value.Double d, Value.Integer i -> 
                    let n, m = d, double i
                    Value.Double (if op = "*" then n*m elif op = "/" then n/m else n-m)
                | _, Value.Nothing | Value.Nothing, _ -> 
                    Value.Nothing
                | _ -> failwith <| sprintf "Operation %s is not applicable to args %A %A" op v1 v2

        let private getLogicalOperation op = 
            match op with
            | ">"  -> (>)   | "<"  -> (<)
            | ">=" -> (>=)  | "<=" -> (<=)
            | "="  -> (=)   | "<>" -> (<>)
            | _ -> failwith <| sprintf "Unknown logical operation %s" op

        let (|C|_|) v =
            match v with
            | Value.Integer i -> Some (i :> IComparable)
            | Value.Double d -> Some (d :> IComparable)
            | Value.Date d -> Some (d :> IComparable)
            | _ -> None

        let applyLogical op v1 v2 =
            match v1, v2 with
            | C n, C m -> 
                Value.Bool <| (getLogicalOperation op) n m

            | Value.Nothing, _ | _, Value.Nothing -> 
                Value.Nothing

            | _ -> failwith <| sprintf "Operation %s is not applicable to args %A %A" op v1 v2
                
        let apply op stack = 
            if op = "_" then
                match stack with
                | (Syntel.Value (Value.Integer i)) :: tail -> 
                    (Syntel.Value (Value.Integer -i)) :: tail 

                | (Syntel.Value (Value.Double d)) :: tail -> 
                    (Syntel.Value (Value.Double -d)) :: tail 

                | head :: tail -> 
                    failwith <| sprintf "Operation - is not applicable to %A" head

                | _ -> failwith "Nothing to negate"

            elif op = "not" then
                match stack with
                | (Syntel.Value (Value.Bool b)) :: tail -> 
                    (Syntel.Value (Value.Bool (not b))) :: tail 

                | head :: _ -> 
                    failwith <| sprintf "Operation 'not' is not applicable to %A" head
                | _ -> 
                    failwith "Nothing to negate"

            elif op |- set ["+"; "-"; "*"; "/"] then
                match stack with
                | (Syntel.Value v1) :: (Syntel.Value v2) :: tail -> 
                    (Syntel.Value <| applyMath op v2 v1) :: tail 

                | _ -> failwith "Need two values to apply mathematical operation"

            elif op |- set [">"; "<"; ">="; "<="; "="; "<>"] then
                match stack with // TODO =, <> are not directly applicable to doubles!
                | (Syntel.Value v1) :: (Syntel.Value v2) :: tail -> 
                    (Syntel.Value <| applyLogical op v2 v1) :: tail 

                | _ -> failwith "Need two values to apply logical operation"

            elif op |- set ["and"; "or"] then
                match stack with
                | (Syntel.Value (Value.Bool b1)) :: (Syntel.Value (Value.Bool b2)) :: tail -> 
                    let o = if op = "and" then (&&) 
                            elif op = "or" then (||) 
                            else failwith <| sprintf "Unknown boolean operation %s" op
                    (Syntel.Value <| Value.Bool (o b1 b2)) :: tail 

                | _ -> failwith "Need two values to apply boolean operation"

            else failwith <| sprintf "Unknown operation %s" op

    module internal Functions = 
        let apply name stack = 
            // functions: 
            //  - control : iif
            //  - math : round, floor, ceil, abs
            //  - strings : format, like, contains
            //  - date : AddDays, AddMonths, AddYears
            //  - rating

            // todo any function with nothing is nothing
            if name = "IIF" then
                match stack with 
                | (Syntel.Value ``if-false``) :: (Syntel.Value ``if-true``) :: (Syntel.Value condition) :: tail -> 
                    match condition with
                    | Value.Bool cond -> Syntel.Value (if cond then ``if-true`` else ``if-false``) :: tail
                    | Value.Nothing -> Syntel.Value condition :: tail
                    | _ -> failwith <| sprintf "Invalid Iif function call. Use Iif(bool, value-if-true, value-if-false)" 
                | _ -> failwith <| sprintf "Invalid Iif function call. Use Iif(bool, value-if-true, value-if-false)" 
            
            elif name = "ROUND" then
                match stack with 
                | (Syntel.Value (Value.Double dbl)) :: tail -> 
                    let value = dbl |> Math.Round
                                    |> int64
                                    |> Value.Integer
                                    |> Syntel.Value
                    value :: tail

                | (Syntel.Value (Value.Integer _)) :: _ | (Syntel.Value (Value.Nothing)) :: _ -> 
                    stack
                                        
                | _ -> failwith <| sprintf "Invalid Round function call. Use Round(double)" 

            elif name |- set ["FLOOR"; "CEIL"; "ABS"] then
                match stack with 
                | (Syntel.Value Value.Nothing) :: tail -> 
                    stack

                | (Syntel.Value (Value.Double dbl)) :: tail -> 
                    let math : float -> float = 
                        match name with
                        | "FLOOR" -> Math.Floor
                        | "CEIL" -> Math.Ceiling
                        | "ABS" -> Math.Abs
                        | _ -> failwith ""

                    let value = dbl |> math
                                    |> int64
                                    |> Value.Integer
                                    |> Syntel.Value

                    value :: tail

                | (Syntel.Value (Value.Integer _)) :: _ -> 
                    stack
                                        
                | _ -> failwith <| sprintf "Invalid %s function call. Use %s(double)" name name

            elif name = "FORMAT" then
                match stack with 
                | (Syntel.Value value) :: (Syntel.Value (Value.String format)) :: tail -> 
                    let res = 
                        match value with
                        | Value.Bool b -> Value.String <| String.Format("{0:" + format + "}", b)
                        | Value.Date d -> Value.String <| String.Format("{0:" + format + "}", d)
                        | Value.Double d -> Value.String <| String.Format("{0:" + format + "}", d)
                        | Value.Integer i -> Value.String <| String.Format("{0:" + format + "}", i)
                        | other -> other
                    Syntel.Value res :: tail

                | Syntel.Value Value.Nothing :: _ :: _ 
                | _ :: Syntel.Value Value.Nothing :: _ -> 
                    stack

                | _ -> failwith "Invalid Format function call. Use Format(format, value)" 

            elif name = "LIKE" then
                match stack with 
                | (Syntel.Value (Value.String str)) :: (Syntel.Value (Value.String regex)) :: tail -> 
                    let m = Regex.Match(str, regex)
                    (Syntel.Value <| Value.Bool m.Success) :: tail

                | Syntel.Value Value.Nothing :: _ :: _ 
                | _ :: Syntel.Value Value.Nothing :: _ -> 
                    stack

                | _ -> failwith "Invalid Like function call. Use Like(str, regex-str)" 

            elif name = "CONTAINS" then
                match stack with 
                | (Syntel.Value (Value.String needle)) :: (Syntel.Value (Value.String haystack)) :: tail -> 
                    (Syntel.Value <| Value.Bool (haystack.Contains(needle))) :: tail

                | Syntel.Value Value.Nothing :: _ :: _ 
                | _ :: Syntel.Value Value.Nothing :: _ -> 
                    stack

                | _ -> failwith "Invalid Contains function call. Use Contains(haystack-string, needle-string)" 
                
            elif name |- set ["ADDDAYS"; "ADDMONTHS"; "ADDYEARS"] then
                match stack with 
                | Syntel.Value Value.Nothing :: _ :: _ 
                | _ :: Syntel.Value Value.Nothing :: _ -> 
                    stack

                | (Syntel.Value (Value.Integer num)) :: (Syntel.Value (Value.Date dt)) :: tail -> 
                    let add = 
                        match name with
                        | "ADDDAYS" -> dt.AddDays << float
                        | "ADDMONTHS" -> dt.AddMonths
                        | "ADDYEARS" -> dt.AddYears
                        | _ -> failwith ""

                    let newDate = num |> int 
                                      |> add 
                                      |> Value.Date 
                                      |> Syntel.Value

                    newDate :: tail

                | _ -> failwith <| sprintf "Invalid %s function call. Use %s(date, int)" name name

            else failwith "Unknown function" 
            
    let private getVariable (vars : (string, obj) Map) var = 
        match var with
        | Variable.Global name -> 
            if vars |> Map.containsKey name then
                Value.interpret vars.[name]
            else Value.Nothing

        | Variable.Object (name, field) -> 
            if vars |> Map.containsKey name then
                let map = vars.[name] :?> (string, obj) Map
                if map |> Map.containsKey field then
                    Value.interpret map.[field]
                else Value.Nothing
            else Value.Nothing

    let evaluateGrammar grammar (vars : (string, obj) Map) = 
        let items = 
            ([], grammar) ||> List.fold (fun progress i -> 
                match i with
                | Syntel.Value v -> 
                    i :: progress

                | Syntel.Variable var -> 
                    (Syntel.Value <| getVariable vars var) :: progress

                | Syntel.Operation op -> 
                    Operations.apply op progress

                | Syntel.Function name -> 
                    Functions.apply name progress) 
        
        match items with 
        | (Syntel.Value v) :: [] -> v
        | _ -> failwith "Invalid result"

    let evaluate code = 
        Lexem.parse code
        ||> Syntan.grammar 
        |> List.map snd 
        |> evaluateGrammar  