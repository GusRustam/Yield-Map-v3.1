namespace YieldMap.Tools
open System.Runtime.InteropServices

[<AutoOpen>]
module Workflows =
    module Attempt = 
        type Attempt<'T> = (unit -> 'T option)

        let succeed x = (fun () -> Some(x)) : Attempt<'T>
        let fail = (fun () -> None) : Attempt<'T>
        let runAttempt (a : Attempt<'T>) = a() 
        let asAttempt (a : 'T option) = fun () -> a
//        let tryAsAttempt f x = try Some(f x) with _ -> None

        let bind p rest = match runAttempt p with None -> fail | Some r -> (rest r)
        let delay f = (fun () -> runAttempt (f())) : Attempt<'T>
        let combine p1 p2 = (fun () -> match p1() with None -> p2() | res -> res)

        type AttemptBuilder() = 
            member b.Bind(p, rest) = bind p rest
            member b.Delay(f) = delay f
            member b.Return(x) = succeed x
            member b.ReturnFrom(x : Attempt<'T>)  = x
            member b.Combine(p1 : Attempt<'T>, p2 : Attempt<'T>) = combine p1 p2
            member b.Zero() = fail
      
        let attempt = AttemptBuilder()

[<RequireQualifiedAccess>]
module Solver = 
    open Workflows
    open Workflows.Attempt
    open System

    let private solve (f:float->float) a b maxlev thX thY = 
        let rec doSolve a b level = attempt {
            if level > maxlev then return! fail
            let fa, fb = f a, f b
            if Math.Sign(fa) = Math.Sign(fb) then return! fail
            let start, finish = if fa < 0.0 then a, b else b, a
            let m = (a+b)/2.0
            let fm = f m

            if Math.Abs(fm) < thY || Math.Abs(finish-start) < thX then
                return m                
            else 
                if fm < 0.0 then
                    return! doSolve m finish (level+1)
                else 
                    return! doSolve start m (level+1)
        }
        doSolve a b 0 |> runAttempt

    let bisect (f:float->float) (a:float) (b:float) = solve f a b 100 1e-4 1e-4

[<RequireQualifiedAccess>]
module ExcelDates = 
    open System

    let toDateTime serialDate = 
        let serialDate = if serialDate > 59 then serialDate - 1 else serialDate
        DateTime(1899, 12, 31).AddDays(float serialDate)
    
[<RequireQualifiedAccess>]
module Location =
    open System.IO
    open System.Reflection

    type private __tag = class end
    let path = Path.GetDirectoryName(Assembly.GetAssembly(typedefof<__tag>).CodeBase).Substring(6)
    let temp = Path.GetTempPath()

[<AutoOpen>]
module Extensions =
    open System
    open System.IO
    open System.Reflection
    open System.Runtime.Serialization
    open System.Runtime.Serialization.Formatters.Binary
    open System.Text

    (* Расширение встроенной функциональности *)
    module Array = 
        let first (arr: 'a array) = arr.[0]
        let others (arr: 'a array) = arr.[1..]
        let split arr = first arr, others arr
        let repeat arr times = [|1..times|] |> Array.fold (fun acc _ -> Array.append acc arr) [||]
        let unique arr = arr |> Seq.distinct |> Seq.toArray

    /// Extension methods to get single attribute in a cleaner way
    type MemberInfo with
        member self.Attr<'T when 'T :> Attribute> () = 
            try Some(self.GetCustomAttribute(typedefof<'T>, false) :?> 'T) 
            with _ -> None

    type Object with
        member self.Serialize() = 
            let formatter = BinaryFormatter()
            use stream = new MemoryStream()
            try 
                formatter.Serialize(stream, self)
                Convert.ToBase64String(stream.ToArray())
            with :? SerializationException -> ""

        member self.AsOption() = match self with null  -> None | _ -> Some(self)
    
    type String with
        member self.Deserialize<'T when 'T : null>() =
            let formatter = BinaryFormatter()
            try
                use stream = new MemoryStream(Convert.FromBase64String(self))
                formatter.Deserialize(stream) :?> 'T
            with :? SerializationException -> null

        member self.DeserializeStruct<'T>() =
            let formatter = BinaryFormatter()
            try
                use stream = new MemoryStream(Convert.FromBase64String(self))
                Some(formatter.Deserialize(stream) :?> 'T)
            with _ -> None

    type Async with
        static member WithTimeout (timeout:TimeSpan option) operation = 
            match timeout with
            | Some(time) -> async { try return Async.RunSynchronously (operation, time.Milliseconds) |> Some with :? TimeoutException -> return None }
            | _ -> async { return operation |> Async.RunSynchronously |> Some }

        static member WithTimeoutEx (timeout:TimeSpan option) operation = 
            match timeout with
            | Some(time) -> async { return Async.RunSynchronously (operation, time.Milliseconds) }
            | _ -> operation

[<AutoOpen>]
module Disposer = 
    open System
    open System.Runtime.InteropServices

    [<AbstractClass>] 
    type Disposer() = 
        abstract member DisposeUnmanaged : unit -> unit
        abstract member DisposeManaged : unit -> unit

        interface IDisposable with
            member self.Dispose() =
                printfn "Dispose()" 
                self.PerformDispose true
                GC.SuppressFinalize self

        override self.Finalize() =
            printfn "Finalize()"
            self.PerformDispose false

        member self.PerformDispose disposeManaged =
            self.DisposeUnmanaged()
            if disposeManaged then 
                self.DisposeManaged()

    type ComDisposer<'T when 'T : equality and 'T : null>(item : 'T) = 
        inherit Disposer()

        let mutable _object = item
        member self.Object() = _object
    
        override self.DisposeUnmanaged() = ()
        override self.DisposeManaged() =
            if _object <> null && Marshal.IsComObject(_object) then
                Marshal.ReleaseComObject(_object) |> printfn "References left: %d" 
                _object <- null

module Logging = 
    open NLog
    open NLog.Config
    open NLog.Layouts
    open NLog.Targets

    open System
    open System.IO
    open System.Threading
    open System.Collections.Generic
    open System.Diagnostics

    (* Basic logging tools: levels and logging interface *)
    type LoggingLevel =
        | Trace = 1
        | Debug = 2
        | Info = 3
        | Warn = 4
        | Error = 5
        | Fatal = 6
        | Off = 7
        
    let AsN = function
        | LoggingLevel.Trace -> LogLevel.Trace
        | LoggingLevel.Debug -> LogLevel.Debug
        | LoggingLevel.Info -> LogLevel.Info
        | LoggingLevel.Warn -> LogLevel.Warn
        | LoggingLevel.Error -> LogLevel.Error
        | LoggingLevel.Fatal -> LogLevel.Fatal
        | _ -> LogLevel.Off

    type Logger = 
        [<Conditional("TRACE")>]
        abstract member Trace : string -> unit

        [<Conditional("TRACE")>]
        abstract member TraceEx : string -> exn -> unit

        [<Conditional("DEBUG")>]
        abstract member Debug : string -> unit

        [<Conditional("DEBUG")>]
        abstract member DebugEx : string -> exn -> unit

        [<Conditional("DEBUG")>]
        abstract member Info : string -> unit

        [<Conditional("DEBUG")>]
        abstract member InfoEx : string -> exn -> unit

        [<Conditional("DEBUG")>]
        abstract member Warn : string -> unit

        [<Conditional("DEBUG")>]
        abstract member WarnEx : string -> exn -> unit

        [<Conditional("DEBUG")>]
        abstract member Error : string -> unit

        [<Conditional("DEBUG")>]
        abstract member ErrorEx : string -> exn -> unit

        [<Conditional("DEBUG")>]
        abstract member Fatal : string -> unit

        [<Conditional("DEBUG")>]
        abstract member FatalEx : string -> exn -> unit

    (* Logging sinks *)
    // todo mutable threshold
    type LoggingSink = 
        abstract member Log : LoggingLevel * string * string -> unit
        abstract member Log : LoggingLevel * string * string * exn -> unit

    (* Creating the loggers *)
    type Sinker = 
        // todo store weak references to the loggers so that to update thresholds
        // todo make a static class to rule it 
        static member consoleSink () = 
            {new LoggingSink with
                member x.Log (level, name, message) = 
                    printfn "%-25s | %-8A | %15s | %s" (DateTime.Now.ToString("dd-MMM-yy hh:mm:ss,fff")) level name message
                
                member x.Log (level, name, message, ex) = 
                    x.Log (level, name, message)
                    printfn "Exception is %s" <| ex.ToString()}
        
        static member nullSink () = 
            {new LoggingSink with
                member x.Log (level, name, message) = ()
                member x.Log (level, name, message, ex) = ()}

        static member nLogSink fileName name = 
            let layoutText = "${date} \t ${level} \t ${callsite:includeSourcePath=false} | ${message} | ${exception:format=Type,Message} | ${stacktrace}"
            let txtTarget = 
                new FileTarget ( 
                    DeleteOldFileOnStartup = true, 
                    Name = "Main",  
                    Layout = Layout.FromString(layoutText), 
                    FileName = Layout.FromString(Path.Combine(Location.temp, fileName)))

            let udpTarget = 
                new ChainsawTarget (
                    Address = Layout.FromString("udp://127.0.0.1:7071"),
                    Name = "Chainsaw",
                    Layout = new Log4JXmlEventLayout())

            let logger = LogManager.GetCurrentClassLogger()
            let loggerConfig = LoggingConfiguration()
            do loggerConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, txtTarget));
            do loggerConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, udpTarget));
            do LogManager.Configuration <- loggerConfig

            let logga = LogManager.GetLogger name

            {new LoggingSink with
                member x.Log (level, _, message) = logga.Log(AsN level, message)
                member x.Log (level, _, message, ex) = logga.LogException(AsN level, message, ex)}

    let globalThreshold = ref LoggingLevel.Trace
    let globalSink = Sinker.nLogSink "yield-map.log"

    type LogFactory() =
        static let loggers = Dictionary()
        static let locker = obj()
        
        static member create (name, threshold) =
            try
                Monitor.Enter locker
                if not <| loggers.ContainsKey name then
                    let create threshold name (sink:LoggingSink) =
                        fun level message -> if level >= !threshold && level >= !globalThreshold then sink.Log (level, name, message)
            
                    let createEx threshold name (sink:LoggingSink)  =
                        fun level message ex -> if level >= !threshold && level >= !globalThreshold then sink.Log (level, name, message, ex)
            
                    let crt = create threshold name (globalSink name)
                    let crtEx = createEx threshold name (globalSink name)
            
                    let newLogger = {
                        new Logger with
                            member x.TraceEx message ex = crtEx LoggingLevel.Trace message ex
                            member x.Trace message = crt LoggingLevel.Trace message
                            member x.DebugEx message ex = crtEx LoggingLevel.Debug message ex
                            member x.Debug message = crt LoggingLevel.Debug message 
                            member x.InfoEx message ex = crtEx LoggingLevel.Info message ex
                            member x.Info message = crt LoggingLevel.Info message 
                            member x.WarnEx message ex = crtEx LoggingLevel.Warn message ex
                            member x.Warn message = crt LoggingLevel.Warn message 
                            member x.ErrorEx message ex = crtEx LoggingLevel.Error message ex
                            member x.Error message = crt LoggingLevel.Error message 
                            member x.FatalEx message ex = crtEx LoggingLevel.Fatal message ex
                            member x.Fatal message = crt LoggingLevel.Fatal message}

                    loggers.Add(name, newLogger)
                    newLogger
                else loggers.[name]
            finally Monitor.Exit locker

        static member create name = LogFactory.create (name, globalThreshold)

[<RequireQualifiedAccess>]
module Ole32 = 
    open Logging

    let logger = LogFactory.create "Ole32"

    [<DllImport("ole32.dll")>] 
    extern void CoUninitialize()

    let killComObject (wut : 'T ref) =
        let he = !wut
        if he <> null && Marshal.IsComObject(he) then
            let refLeft = Marshal.ReleaseComObject(he) 
            refLeft |> sprintf "References left: %d" |> logger.Trace
            wut := null