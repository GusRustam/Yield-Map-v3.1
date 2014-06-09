namespace YieldMap.Language

open System

module Stuff =
    type Rating = class end 

    type RegexAttribute(expr : string) = 
        inherit Attribute()
        member x.Expr = expr

    type Value = 
    | String of string
    | Integer of int
    | Double of double
    | Rating of Rating
    | Bool of bool
    | Date of DateTime

    type Variable = 
    | Object of string * string
    | Plain of string

    type UnaryOperation =
    | Negate
    | Not

    type BinaryOperation =
    | Add
    | Subtract
    | Multiply
    | Divide
    | Equals
    | NotEquals
    | Greater
    | Less
    | GreaterOrEqual
    | LessOrEqual
    | And 
    | Or

    type Params = Exactly of int | Unlimited

    type FCall = {
        name : string
        parameters : Params
        eval : (obj -> obj)
    }

    let functions = [|
        {name = "Like"; parameters = Unlimited; eval = fun _ -> obj() }
    |]

    type FunctionCall =
    | Like 
    | Round
    | Mod
    | Rem

    type Operation = 
    | [<Regex("")>] UnaryOperation of UnaryOperation
    | [<Regex("")>] BinaryOperation of BinaryOperation
    | [<Regex("")>] FunctionCall of FunctionCall

    type Lexem = 
    | Value of Value
    | Variable of Variable
    | Operation of Operation

    (*
     * Hihi hoho
     *)

module Parser =
    type State = 
    | Expr
    | Term
    | Value
    | Variable
    | Call
    | Operation

    type Rating = class end 
 
    type Value = 
    | String of string
    | Integer of int
    | Double of double
    | Rating of Rating
    | Bool of bool
    | Date of DateTime

    type Variable = 
    | Object of string * string
    | Plain of string

    type Operation = 
    | UnaryOperation of string
    | BinaryOperation of string
    | FunctionCall of string

    type Lexem = 
    | Value of Value
    | Variable of Variable
    | Operation of Operation
    with static member classify x = Lexem.Value (Value.String "")

    type Status = {
        position : int
        stack : Lexem list
        state : State
    }

    module Helper = 
        let trimStart (str:string) =
            let rec tS (str:string) pos = if str.[0] = ' ' then tS (str.Substring(1)) (pos+1) else (str, pos)
            tS str 0

    let parse (s : string) = 
        let initialStatus = { 
            position = 0
            stack = []
            state = Expr
        }

        let rec prs str (status:Status) = 
            if String.IsNullOrWhiteSpace str then 
                status
            else
                let (str, position) = Helper.trimStart str

                match Lexem.classify str with
                | Value v -> prs str { status with state = State.Value; position = position }
                | _ -> prs str status

            
        prs s initialStatus