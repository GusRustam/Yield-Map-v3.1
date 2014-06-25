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
   
