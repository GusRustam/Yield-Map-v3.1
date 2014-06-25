namespace YieldMap.Tools

open System

module Ratings = 
    type Agency = 
    | SnP
    | Moodys
    | Fitch

    [<CustomEquality; CustomComparison>]
    type Notch = 
        { 
            level : int
            names : string Set
        }
        with
            override x.Equals(y) = 
                if y :? Notch then
                    let n = y :?> Notch
                    match x, n with
                    | { level = l1 }, { level = l2 } when l1 = l2 -> true
                    | _ -> false
                else false

            override x.GetHashCode() = match x with { level = l  } -> l

            interface Notch IComparable with
                member x.CompareTo(y) = 
                    match x, y with 
                    | { level = l1 }, { level = l2 } -> l1.CompareTo l2

            static member Notches = [
                { level = 21; names = set ["AAA"]}
                { level = 20; names = set ["AA+"; "Aa1"]}
                { level = 19; names = set ["AA"; "Aa2"]}
                { level = 18; names = set ["AA-"; "Aa3"]}
                { level = 17; names = set ["A+"; "A1"]}
                { level = 16; names = set ["A"; "A2"]}
                { level = 15; names = set ["A-"; "A3"]}
                { level = 14; names = set ["BBB+"; "Baa1"]}
                { level = 13; names = set ["BBB"; "Baa2"]}
                { level = 12; names = set ["BBB-"; "Baa3"]}
                { level = 11; names = set ["BB+"; "Ba1"]}
                { level = 10; names = set ["BB"; "Ba2"]}
                { level = 9; names = set ["BB-"; "Ba3"]}
                { level = 8; names = set ["B+"; "B1"]}
                { level = 7; names = set ["B"; "B2"]}
                { level = 6; names = set ["B-"; "B3"]}
                { level = 5; names = set ["CCC+"; "Caa1"]}
                { level = 4; names = set ["CCC"; "Caa2"]}
                { level = 3; names = set ["CCC-"; "Caa3"]}
                { level = 2; names = set ["CC"; "Ca"]}
                { level = 1; names = set ["C"]}
                { level = 0; names = set [""]}]

    [<StructuralEquality; CustomComparison>]
    type RatingInfo = 
        {
            notch : Notch
            agency : Agency
            date : DateTime 
        }
       
        interface RatingInfo IComparable with
            member x.CompareTo(y) =
                match x, y with
                |  { notch = n1; agency = a1; date = d1 }, 
                   { notch = n2; agency = a2; date = d2 } -> 
                    let dc = d1.CompareTo(d2)
                    if dc = 0 then
                        let nc1 = n1 :> Notch IComparable
                        nc1.CompareTo(n2)
                    else dc