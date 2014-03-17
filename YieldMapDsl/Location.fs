namespace YieldMap.Tools 
    
[<RequireQualifiedAccess>]
module Location =
    open System.IO
    open System.Reflection

    type private __tag = class end
    let path = Path.GetDirectoryName(Assembly.GetAssembly(typedefof<__tag>).CodeBase).Substring(6)
    let temp = Path.GetTempPath()