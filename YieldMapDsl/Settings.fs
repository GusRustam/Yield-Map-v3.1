namespace YieldMap.Settings

[<AutoOpen>]
module Settings =

    // todo ORM mapping or XML mapping or whatever mapping
    type QuoteSettings = {
        midIfBoth : bool
    }

    type LoggingSettings = {
        fileName : string
    }

    type SourceSettings = {
        defaultFeed : string
    }

    type Settings = {
        logging : LoggingSettings
        quotes : QuoteSettings
        source : SourceSettings
    }

    let globalSettings = ref {
        logging = {fileName = "yield-map.log"}
        quotes = {midIfBoth = true}
        source = {defaultFeed = "IDN"}
    }