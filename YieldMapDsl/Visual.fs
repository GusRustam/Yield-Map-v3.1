module Visual
    open System
    module Ranges =
            type Limit<'T> = 
            | Auto              // автоматическая граница - по минимуму или максимуму
            | Fix of 'T         // фиксированная граница
            | Limit of 'T       // наибольшее / наименьшее значение, не заходящее за границу

            type Limit = 
                static member greater<'T when 'T : comparison> (limit:Limit<'T>) (point : 'T) = 
                    match limit with Auto -> true | Fix(x) | Limit(x) -> point >= x
                static member less<'T when 'T : comparison> (limit:Limit<'T>) (point : 'T) = 
                    match limit with Auto -> true | Fix(x) | Limit(x) -> point <= x

            type Interval = Day | Week | Month | Year
            type Percentage = Percent | PercentPoint | BasisPoint

            type Range<'T when 'T : comparison> = { Since : Limit<'T>;  Till : Limit<'T> }
                with member x.inside (point:'T) = Limit.greater x.Since point && Limit.less x.Till point

            // Samples:
            //  AbsoluteDate({Since = Fix(DateTime.Today); Till = Fix(DateTime.Today.AddYears(20))})
            //  Percentage({Since = Fix 0M; Till = Auto}, Percent)
            //  DateInvterval({Since = Fix 0; Till = Fix 10}, Year)
            //  Percentage({Since = Auto; Till = Limit 2000M}, BasisPoint)
            //  DateInvterval({Since = Fix 0; Till = Fix 10}, Year)
            //  Percentage({Since = Auto; Till = Limit 2000M}, BasisPoint)
            type Dimension = 
            | AbsoluteDate of Range<DateTime> 
            | DateInvterval of Range<int> * Interval 
            | Percentage of Range<decimal> * Percentage