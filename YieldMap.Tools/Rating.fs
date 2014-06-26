namespace YieldMap.Tools

open System

open Response

module Ratings = 
    open YieldMap.Tools.Aux

    [<CustomEquality; CustomComparison>]
    type Notch = { name : string; level : int }
        with
            override x.Equals(y) = 
                if y :? Notch then
                    let y = y :?> Notch
                    let { level = l1; name = n1 } = x
                    let { level = l2; name = n2 } = y
                    l1 = l2 && n1 = n2
                else false

            override x.GetHashCode() = 
                let { level = l1; name = n1 } = x
                37 * (l1 + 37 * n1.GetHashCode() + 23)

            interface IComparable with
                member x.CompareTo(y) = 
                    if y :? Notch then
                        let y = y :?> Notch
                        match x, y with 
                        | { level = l1 }, { level = l2 } -> l1.CompareTo l2
                    else 1

            static member byName n =
                match Notch.findLevel n with
                | Some l -> { name = n; level = l}
                | None -> failwith <| sprintf "Notch with name %s not found" n

            static member private notches = [
                27, set ["AAA"]
                25, set ["AA+"; "Aa1"]
                24, set ["AA"; "Aa2"]
                23, set ["AA-"; "Aa3"]
                22, set ["A+"; "A1"]
                21, set ["A"; "A2"]
                20, set ["A-"; "A3"]
                19, set ["BBB+"; "Baa1"]
                18, set ["BBB"; "Baa2"]
                17, set ["BBB-"; "Baa3"]
                16, set ["BB+"; "Ba1"]
                15, set ["BB"; "Ba2"]
                14, set ["BB-"; "Ba3"]
                13, set ["B+"; "B1"]
                12, set ["B"; "B2"]
                11, set ["B-"; "B3"]
                10, set ["CCC+"; "Caa1"]
                9, set ["CCC"; "Caa2"]
                8, set ["CCC-"; "Caa3"]
                7, set ["CC"; "Ca"]
                4, set ["C"]
                3, set ["SD"; "D"]
            ]

            static member private notchMap = Notch.notches |> Map.ofList

            static member private findLevel name = 
                let res = 
                    Notch.notches
                    |> List.tryFind (fun (_, s) -> s |> Set.contains name) 
                match res with
                | Some (level, _) -> Some level
                | None -> None 

            static member private findName level = 
                if Notch.notchMap |> Map.containsKey level then
                    let res = Notch.notchMap.[level]
                              |> Set.toArray
                    Some <| res.[0]
                else None

            static member parse n =
                let l = Notch.findLevel n
                match l with
                | Some l -> Answer { name = n; level = l }
                | None -> Failure <| Problem (sprintf "%s not a rating" n)

            static member elevate shift (n & { level = l }) = 
                let n = Notch.findName (l + shift)
                match n with
                | Some n -> Answer {name = n; level = l + shift }
                | None -> Failure <| Problem (sprintf "Invalid shift %d for rating %A" shift n)

    type Rating = {
        notch : Notch
        date : DateTime
    }

//    module Parser = 
//        let private fromAgencyCode c =
//            match c with
//            | "SPI" | "S&P" -> SnP, Long, Foreign
//            | "MDL" | "MIS" | "MDY" -> Moodys, Long, Foreign
//            | "FTC" | "FDL" | "FSU" -> Fitch, Long, Foreign
//            | _ -> failwith <| sprintf "Invalid code %s" c
//
//        let private fromName n a t k d =
//            if a |- set [SnP; Moodys; Fitch] && t = Long && k = Foreign then
//                let l = 
//                    if n = "AAA" then 21
//                    elif n |- set ["AA"; "Aa2"] then 19
//                    elif n |- set ["AA-"; "Aa3"] then 18
//                    elif n |- set ["A+"; "A1"] then 17
//                    elif n |- set ["A"; "A2"] then 16
//                    elif n |- set ["A-"; "A3"] then 15
//                    elif n |- set ["BBB+"; "Baa1"] then 14
//                    elif n |- set ["BBB"; "Baa2"] then 13
//                    elif n |- set ["BBB-"; "Baa3"] then 12
//                    elif n |- set ["BB+"; "Ba1"] then 11
//                    elif n |- set ["BB"; "Ba2"] then 10
//                    elif n |- set ["BB-"; "Ba3"] then 9
//                    elif n |- set ["B+"; "B1"] then 8
//                    elif n |- set ["B"; "B2"] then 7
//                    elif n |- set ["B-"; "B3"] then 6
//                    elif n |- set ["CCC+"; "Caa1"] then 5
//                    elif n |- set ["CCC"; "Caa2"] then 4
//                    elif n |- set ["CCC-"; "Caa3"] then 3
//                    elif n |- set ["CC"; "Ca"] then 2
//                    elif n = "C" then 1
//                    else 0
//                { agency = a; term = t; kind = k; name = n; level = l; date = d }
//            else failwith ""
//
//        let elevate node offset = ()
//
//        let parse name code = code |> fromAgencyCode
//                                   |||> fromName name



//    type Agency = SnP | Moodys | Fitch
//        with 
//            override x.ToString() = Agency.toString x
//            static member abbr a =
//                match a with
//                | SnP -> "SNP"
//                | Moodys -> "MDY"
//                | Fitch -> "FTC"
//            static member toString a =
//                match a with
//                | SnP -> "S&P"
//                | Moodys -> "Moody's"
//                | Fitch -> "Fitch"


//module Ratings = 
//    [<CustomEquality; CustomComparison>]
//    type Notch = 
//        { 
//            level : int
//            names : string Set
//        }
//        with
//            override x.Equals(y) = 
//                if y :? Notch then
//                    let n = y :?> Notch
//                    match x, n with
//                    | { level = l1 }, { level = l2 } when l1 = l2 -> true
//                    | _ -> false
//                else false
//
//            override x.GetHashCode() = match x with { level = l  } -> l
//
//            interface Notch IComparable with
//                member x.CompareTo(y) = 
//                    match x, y with 
//                    | { level = l1 }, { level = l2 } -> l1.CompareTo l2
//
//            static member Notches = [
//                { level = 21; names = set ["AAA"]}
//                { level = 20; names = set ["AA+"; "Aa1"]}
//                { level = 19; names = set ["AA"; "Aa2"]}
//                { level = 18; names = set ["AA-"; "Aa3"]}
//                { level = 17; names = set ["A+"; "A1"]}
//                { level = 16; names = set ["A"; "A2"]}
//                { level = 15; names = set ["A-"; "A3"]}
//                { level = 14; names = set ["BBB+"; "Baa1"]}
//                { level = 13; names = set ["BBB"; "Baa2"]}
//                { level = 12; names = set ["BBB-"; "Baa3"]}
//                { level = 11; names = set ["BB+"; "Ba1"]}
//                { level = 10; names = set ["BB"; "Ba2"]}
//                { level = 9; names = set ["BB-"; "Ba3"]}
//                { level = 8; names = set ["B+"; "B1"]}
//                { level = 7; names = set ["B"; "B2"]}
//                { level = 6; names = set ["B-"; "B3"]}
//                { level = 5; names = set ["CCC+"; "Caa1"]}
//                { level = 4; names = set ["CCC"; "Caa2"]}
//                { level = 3; names = set ["CCC-"; "Caa3"]}
//                { level = 2; names = set ["CC"; "Ca"]}
//                { level = 1; names = set ["C"]}
//                { level = 0; names = set [""]}]
//
//    [<StructuralEquality; CustomComparison>]
//    type RatingInfo = 
//        {
//            notch : Notch
//            agency : string
//            date : DateTime 
//        }
//       
//        interface RatingInfo IComparable with
//            member x.CompareTo(y) =
//                let { notch = n1; agency = a1; date = d1 } = x
//                let { notch = n2; agency = a2; date = d2 } = y
//
//                let dc = d1.CompareTo d2
//                if dc = 0 then
//                    let nc = (n1 :> Notch IComparable).CompareTo n2
//                    if nc = 0 then
//                        a1.CompareTo a2
//                    else nc
//                else dc
