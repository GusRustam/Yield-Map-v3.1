namespace YieldMap.Core.Notifier

[<AutoOpen; RequireQualifiedAccess>]
module Notifier = 
    open YieldMap.Core.Responses
    type Severity = Note | Warn | Evil

    let private n = Event<string * Failure * Severity> ()

    let notify (source, message, severity) = n.Trigger (source, message, severity)
    let notification = n.Publish

[<RequireQualifiedAccess>]
module Perquisition = 
    type Answer = Yes | No | Retry | Ok | Cancel
    type Kind = Inform | Confirm | Choose

    type Request = 
        {
            kind    : Kind
            message : string
            options : Answer array
            implied : Answer option
            timeout : int option
        }
        with    
            static member create (_kind, _message, _options) = 
                { kind = _kind; message = _message; options = _options; implied = None; timeout = None }
            static member create (_kind, _message, _options, _implied, _timeout) = 
                { kind = _kind; message = _message; options = _options; implied = _implied; timeout = _timeout }
    
    type Inquiry = abstract member Ask : Request -> Answer Async