#if INTERACTIVE
#r "System"
#r "mscorlib"
#r @"C:\Users\Rustam Guseynov\AppData\Local\Thomson Reuters\TRD 6\Program\Interop.EikonDesktopDataAPI.dll"
#r @"C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map\Tools\YieldMapDsl\YieldMapDsl\bin\Debug\YieldMapDsl.dll"
#endif

open Core.Printf

let q format = 
    let cont z =
        // Some work with formatted string
        printf "%s" z 
    Printf.kprintf cont format 

q "1"
q "%s 1" "aaa"
q "%A %d 1" 123123 12