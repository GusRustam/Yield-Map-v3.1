namespace YieldMap.Calculations

open YieldMap.Tools
open YieldMap.Tools.Logging
 
open AdfinXAnalyticsFunctions

open System
open System.Collections.Generic
open System.IO

[<AutoOpenAttribute>]
module Calculations = 
    open AdfinXAnalyticsFunctions

    open YieldMap.Loading
    open YieldMap.Tools.Workflows
    open YieldMap.Tools.Workflows.Attempt

    [<RequireQualifiedAccess>]
    module Bonds = 
        type YieldToWhat = 
            YTM | YTP | YTC | YTB | YTW | YTA | Perpetual
            static member tryParse (s:string) =
                match s.ToUpper().Trim() with
                | "YTM" | "Maturity" -> Some(YTM)
                | "YTP" | "Put" -> Some(YTP)
                | "YTC" | "Call" -> Some(YTC)
                | "YTB" -> Some(YTB)
                | "YTW" -> Some(YTW)
                | "YTA" -> Some(YTA)
                | "Perpetual" -> Some(Perpetual)
                | _ -> None
            
        // todo spread duration, key rate duration
        // todo yield, duration -> another currency !!!!

        type StraightRequest = {
            BS : string           // bond structure
            RS : string           // rate structure
            DT : DateTime         // calc date
            CPN : decimal         // coupon rate
            MTY : DateTime option // maturity date; supports perpetuals
            STL : int             // settle date
            
            PRC : decimal option  // price
            YLD : decimal option  // yield
        } with 
            member r.Yield = r.YLD |> asAttempt
            member r.Price = r.PRC |> asAttempt

        type FrnRequest = {
            FS : string           // bond structure
            RS : string           // rate structure
            DT : DateTime         // calc date
            MTY : DateTime option // maturity date; supports perpetuals
            STL : int             // settle date

            CI : decimal          // current index
            PI : decimal          // project index
            
            PRC : decimal option  // price
            YLD : decimal option  // yield
        } with 
            member r.Yield = r.YLD |> asAttempt
            member r.Price = r.PRC |> asAttempt

        type YieldInfo = {
            Yield : decimal
            ToWhat : YieldToWhat
            ToDate : DateTime option
        }

        type DurationInfo = {
            Effective : decimal
            Macauley : decimal
            Modified : decimal
            AverageLife : decimal option // perpetuals do not have average life
            Convexity : decimal
            PVPB : decimal
        }

        //---------------
        let getValue str f = attempt {
            let (success, res) = f str
            if not success then return! fail
            return res
        }
        let getFloat str = getValue str Double.TryParse
        let getDecimal str = getValue str Decimal.TryParse
        let getInt str =  getValue str Int32.TryParse
        let getYtw str = asAttempt (YieldToWhat.tryParse str)
        let getNotNull v = attempt {
            if v = null then return! fail
            return v
        }
        //---------------

        type GroupCalc = 
            abstract member Bootstrap : unit -> unit
            abstract member Interpolate : unit -> unit
            abstract member ToXY : unit -> unit

        /// Handles straight, putable/callable (european) and perpetual bonds
        type StraightCalc (factory : SdkFactory.Loader) = 
            let bondModule = factory.CreateAdxBondModule()

//            abstract member PointSpread : SpreadRequest * Curve -> decimal option
//            abstract member ZSpread : SpreadRequest * Curve -> decimal option
//            abstract member AswSpread : SpreadRequest * Curve -> decimal option
//            abstract member OaSpread : SpreadRequest * Curve -> decimal option

            member x.Settle (d, s) = bondModule.BdSettle(d, s)
            member x.Yield (yr:StraightRequest) = 
                try
                    attempt {
                        let matDate = match yr.MTY with Some(x) -> box x | None -> null
                        let! p = yr.Price
                        let! yields = bondModule.AdBondYield(yr.STL, float p, matDate, float yr.CPN, yr.BS, yr.RS, "") :?> Array |> getNotNull
                        let rowNum = yields.GetLowerBound(0)

                        let! rate = yields.GetValue(rowNum, 1).ToString() |> getDecimal
                        let! serial = yields.GetValue(rowNum, 2).ToString() |> getInt

                        let toDate = ExcelDates.toDateTime(serial)
                        let! toWhat = yields.GetValue(rowNum, 4).ToString() |> getYtw

                        return {
                            Yield = rate
                            ToWhat = toWhat
                            ToDate = Some(toDate)
                        }
                    } |> runAttempt
                with _ -> None

            member x.Price (pr:StraightRequest) = 
                try
                    attempt {
                        let! y = pr.Yield
                        let matDate = match pr.MTY with Some(x) -> box x | None -> null

                        let res = bondModule.AdBondPrice(pr.STL, float y, matDate, float pr.CPN, 0.0, pr.BS, pr.RS, "", "") :?> Array
                        return! res.GetValue(1, 1).ToString() |> getDecimal
                    } |> runAttempt
                with _ -> None
                
            member x.Durations (dr:StraightRequest) = 
                try
                    attempt {
                        let! y = dr.Yield
                        let matDate = match dr.MTY with Some(x) -> box x | None -> null
                    
                        let! derivatives = bondModule.AdBondDeriv(dr.STL, y, matDate, float dr.CPN, 0.0, dr.BS, dr.RS, "", "") :?> Array |> getNotNull
                            
                        let rowNum = derivatives.GetLowerBound(0)
                        let! modDur = derivatives.GetValue(rowNum, 3).ToString() |> getDecimal
                        let! pvbp = derivatives.GetValue(rowNum, 4).ToString() |> getDecimal
                        let! macDur = derivatives.GetValue(rowNum, 5).ToString() |> getDecimal
                        let avgLife = derivatives.GetValue(rowNum, 6).ToString() |> getDecimal |> runAttempt // !!
                        let! cvxty = derivatives.GetValue(rowNum, 7).ToString() |> getDecimal

                        let price delta = x.Price {dr with YLD = Some (y + delta)} |> asAttempt
                        
                        let delta = 1e-4M
                        let! pMinus = price -delta
                        let! pPlus = price delta
                        let effDur = (pMinus - pPlus) / 2.0M * delta

                        return {
                            Effective = decimal effDur
                            Macauley = decimal macDur
                            Modified = decimal modDur
                            AverageLife = avgLife
                            Convexity = cvxty
                            PVPB = pvbp
                        }
                    } |> runAttempt
                with _ -> None

        type FrnCalc(factory : SdkFactory.Loader) = 
            let bondModule = factory.CreateAdxBondModule()
            member x.Settle (d, s) = bondModule.BdSettle(d, s)
            member x.Yield fr = 
                try
                    attempt {
                        let matDate = match fr.MTY with Some(x) -> box x | None -> null
                        let! p = fr.Price
                        let! yields = bondModule.AdFrnYield(fr.STL, float p, matDate, 0.0, fr.CI, fr.PI, fr.FS, fr.RS, "", "") :?> Array |> getNotNull
                        let rowNum = yields.GetLowerBound(0)

                        let! rate = yields.GetValue(rowNum, 1).ToString() |> getDecimal
                        let! serial = yields.GetValue(rowNum, 2).ToString() |> getInt

                        let toDate = ExcelDates.toDateTime(serial)
                        let! toWhat = yields.GetValue(rowNum, 4).ToString() |> getYtw

                        return {
                            Yield = rate
                            ToWhat = toWhat
                            ToDate = Some(toDate)
                        }
                    } |> runAttempt
                with _ -> None

            member x.Price (pr:FrnRequest) = 
                try
                    attempt {
                        let! y = pr.Yield
                        let matDate = match pr.MTY with Some(x) -> box x | None -> null

                        let res = bondModule.AdFrnPrice(pr.STL, float y, matDate, 0.0, 0.0, float pr.CI, pr.PI, pr.FS, pr.RS, "", "") :?> Array
                        return! res.GetValue(1, 1).ToString() |> getDecimal
                    } |> runAttempt
                with _ -> None

            member x.Durations (dr:FrnRequest) = 
                try
                    attempt {
                        let matDate = match dr.MTY with Some(x) -> box x | None -> null
                        let! y = dr.Yield
                    
                        let! derivatives = bondModule.AdFrnDeriv(dr.STL, y, matDate, 0.0, 0.0, dr.CI, dr.PI, dr.FS, dr.RS, "", "") :?> Array |> getNotNull
                        
                        // AdBondDeriv returns 
                        // 1. Price
                        // 2. OptionFreePrice
                        // 3. Spread Duration
                        // 4. Index Duration
                        // 5. PVBP
                        // 6. Duration
                        // 7. Average Life
                        // 8. Convexity
                        // 9. YTW/YTB Date

                        let rowNum = derivatives.GetLowerBound(0)
                        let! modDur = derivatives.GetValue(rowNum, 4).ToString() |> getDecimal // todo Index Duration is true IR sensitivity!
                        let! pvbp = derivatives.GetValue(rowNum, 5).ToString() |> getDecimal
                        let! macDur = derivatives.GetValue(rowNum, 6).ToString() |> getDecimal
                        let avgLife = derivatives.GetValue(rowNum, 7).ToString() |> getDecimal |> runAttempt
                        let! cvxty = derivatives.GetValue(rowNum, 8).ToString() |> getDecimal

                        let price delta = x.Price {dr with YLD = Some (y + delta)} |> asAttempt
                      
                        let delta = 1e-4M
                        let! pMinus = price -delta
                        let! pPlus = price delta
                        let effDur = (pMinus - pPlus) / 2.0M * delta

                        return {
                            Effective = decimal effDur
                            Macauley = decimal macDur
                            Modified = decimal modDur
                            AverageLife = avgLife
                            Convexity = cvxty
                            PVPB = pvbp
                        }
                    } |> runAttempt
                with _ -> None