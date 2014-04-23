namespace YieldMap.Core.Portfolio

[<AutoOpen>]
module Portfolio =

    // todo ??
    type DbRefresher () =
        member x.NeedsUpdate dt = false
        member x.Chains () = []
        member x.StandaloneRics () = []
        member x.RicsToUpdate () = []

        // Stages: 
        // 1) Reexpand all chains
        // 2) Create a list of all rics to be reloaded
        //    2.1) Include all new rics
        //    2.2) Include all rics which have Last/Next OptionDate equal to today +/- several days
                
