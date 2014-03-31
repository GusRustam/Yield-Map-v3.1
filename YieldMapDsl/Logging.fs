namespace YieldMap.Logging

[<AutoOpen>]
module Logging = 
    open NLog
    open NLog.Config
    open NLog.Layouts
    open NLog.Targets

    open YieldMap.Tools

    open Core.Printf

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
        [<Conditional("TRACE")>] abstract member Trace : string -> unit
        [<Conditional("TRACE")>] abstract member TraceEx : string -> exn -> unit

        [<Conditional("DEBUG")>] abstract member Debug : string -> unit
        [<Conditional("DEBUG")>] abstract member DebugEx : string -> exn -> unit

        [<Conditional("DEBUG")>] abstract member Info : string -> unit
        [<Conditional("DEBUG")>] abstract member InfoEx : string -> exn -> unit

        [<Conditional("DEBUG")>] abstract member Warn : string -> unit
        [<Conditional("DEBUG")>] abstract member WarnEx : string -> exn -> unit

        [<Conditional("DEBUG")>] abstract member Error : string -> unit
        [<Conditional("DEBUG")>] abstract member ErrorEx : string -> exn -> unit

        [<Conditional("DEBUG")>] abstract member Fatal : string -> unit
        [<Conditional("DEBUG")>] abstract member FatalEx : string -> exn -> unit

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
                            member x.Trace message = crt LoggingLevel.Trace <| message

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