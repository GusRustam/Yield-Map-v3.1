namespace YieldMap.Core.Notifier

[<AutoOpen; RequireQualifiedAccess>]
module Notifier = 
    open YieldMap.Core.Responses
    type Severity = Note | Warn | Evil

    let private n = Event<string * Failure * Severity> ()

    let notify (source, message, severity) = n.Trigger (source, message, severity)
    let notification = n.Publish

