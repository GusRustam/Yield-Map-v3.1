//#if INTERACTIVE
//#r "System"
//#r "mscorlib"
//#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
//#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Tools\bin\debug\YieldMap.Tools.dll"
//#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Core\bin\debug\YieldMap.Core.dll"
//#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\packages\Autofac.3.5.0\lib\net40\Autofac.dll"
//#endif
//

module TestFor10 =
    let combinations array value =
        let filterByValue value = List.filter (fun x -> x <= value)
        let rec doCombinations array value acc found = 
            if found then
                true, acc
            else
                let array = array |> filterByValue value
                match array with
                | v :: others -> 
                    if v = value then
                        true, v :: acc
                    elif v > value then
                        false, []
                    else doCombinations others (value - v) (v :: acc) false
                | [] -> false, []
        
        let rec performCombinations array value acc = 
            match array with
            | _ :: others ->
                match doCombinations others value [] false with 
                | false, _ -> performCombinations others value acc
                | true, items -> performCombinations others value (items :: acc)
            | [] -> acc


        match doCombinations array value [] false with 
        | false, _ -> performCombinations array value []
        | true, items -> performCombinations array value (items :: [])


    combinations [1; 2; 3] 6

//        performCombinations array value []
        
            