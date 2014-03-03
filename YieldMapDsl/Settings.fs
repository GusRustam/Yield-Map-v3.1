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

    type Settings = {
        logging : LoggingSettings
        quotes : QuoteSettings
    }

    let globalSettings = {
        logging = {fileName = "yield-map.log"}
        quotes = {midIfBoth = true}
    }