namespace YieldMap.Requests.MetaTables

[<AutoOpen>]
module MetaTables = 
    open System
    open YieldMap.Requests.Attributes
    open YieldMap.Requests.Converters

    [<Request("RH:In")>]
    type BondDescr() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.X.ADF_BondStructure")>] 
        member val BondStructure = String.Empty with get, set

        [<Field(2, "EJV.X.ADF_RateStructure")>] 
        member val RateStructure = String.Empty with get, set

        [<Field(3, "EJV.C.Description")>] 
        member val Description = String.Empty with get, set

        [<Field(4, "EJV.C.OriginalAmountIssued", typeof<OptionalInt64Converter>)>] 
        member val IssueSize : int64 Nullable = Nullable() with get, set

        [<Field(5, "EJV.C.IssuerName")>] 
        member val IssuerName = String.Empty with get, set

        [<Field(6, "EJV.C.BorrowerName")>] 
        member val BorrowerName = String.Empty with get, set

        [<Field(7, "EJV.X.ADF_Coupon", typeof<OptionalFloatConverter>)>] 
        member val Coupon : float Nullable = Nullable() with get, set

        [<Field(8, "EJV.C.IssueDate", typeof<OptionalDateConverter>)>] 
        member val Issue : DateTime Nullable = Nullable() with get, set

        [<Field(9, "EJV.C.MaturityDate", typeof<OptionalDateConverter>)>] 
        member val Maturity : DateTime Nullable = Nullable() with get, set

        [<Field(10, "EJV.C.Currency")>] 
        member val Currency = String.Empty with get, set

        [<Field(11, "EJV.C.ShortName")>] 
        member val ShortName = String.Empty with get, set

        [<Field(12, "EJV.C.IsCallable", typeof<RequiredBoolConverter>)>] 
        member val IsCallable = false with get, set

        [<Field(13, "EJV.C.IsPutable", typeof<RequiredBoolConverter>)>] 
        member val IsPutable = false with get, set

        [<Field(14, "EJV.C.IsFloater", typeof<RequiredBoolConverter>)>] 
        member val IsFloater = false with get, set

        [<Field(15, "EJV.C.IsConvertible", typeof<RequiredBoolConverter>)>] 
        member val IsConvertible = false with get, set

        [<Field(16, "EJV.C.IsStraight", typeof<RequiredBoolConverter>)>] 
        member val IsStraight = false with get, set

        [<Field(17, "EJV.C.Ticker")>] 
        member val Ticker = String.Empty with get, set

        [<Field(18, "EJV.C.Series")>] 
        member val Series = String.Empty with get, set

        [<Field(19, "EJV.C.BorrowerCntyCode")>] 
        member val BorrowerCountry = String.Empty with get, set

        [<Field(20, "EJV.C.IssuerCountry")>] 
        member val IssuerCountry = String.Empty with get, set

        [<Field(21, "RI.ID.ISIN")>]
        member val Isin = String.Empty with get, set

        [<Field(22, "EJV.C.ParentTicker")>]
        member val ParentTicker = String.Empty with get, set

        [<Field(23, "EJV.C.SeniorityTypeDescription")>]
        member val Seniority = String.Empty with get, set

        [<Field(24, "EJV.C.SPIndustryDescription")>]
        member val Industry = String.Empty with get, set

        [<Field(25, "EJV.C.SPIndustrySubDescription")>]
        member val SubIndustry = String.Empty with get, set

        [<Field(26, "EJV.C.InstrumentTypeDescription")>]
        member val Instrument = String.Empty with get, set

        [<Field(27, "EJV.C.NextPayDate", typeof<OptionalDateConverter>)>]
        member val NextCoupon : DateTime Nullable = Nullable() with get, set
        
        override self.ToString() = self.ShortName

    [<Request("D:1984;2020", "RH:In;D")>]
    type CouponData() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "", typeof<OptionalDateConverter>)>]
        member val Date : DateTime Nullable = Nullable() with get, set

        [<Field(2, "EJV.C.CouponRate", typeof<RequiredFloatConverter>)>]
        member val Rate = 0.0 with get, set

        override self.ToString() = sprintf "%s %A %f" self.Ric self.Date self.Rate

    [<Request("RTS:FDL;SPI;MIS;MDL;FIS RTSC:FRN", "RH:In")>] 
    type IssueRatingData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.IR.Rating", typeof<RequiredConverter>)>]
        member val Rating = String.Empty with get, set

        [<Field(2, "EJV.IR.RatingDate", typeof<OptionalDateConverter>)>]
        member val RatingDate : DateTime Nullable = Nullable() with get, set

        [<Field(3, "EJV.IR.RatingSourceCode")>]
        member val RatingSourceCode = String.Empty with get, set

    [<Request("RTSRC:S&P;MDY;FTC", "RH:In")>]
    type IssuerRatingData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.GR.Rating", typeof<RequiredConverter>)>]
        member val Rating = String.Empty with get, set

        [<Field(2, "EJV.GR.RatingDate", typeof<OptionalDateConverter>)>]
        member val RatingDate : DateTime Nullable = Nullable() with get, set

        [<Field(3, "EJV.GR.RatingSourceCode")>]
        member val RatingSourceCode = String.Empty with get, set

    [<Request("RH:In")>]
    type FrnData() = 
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1, "EJV.X.FRNFLOOR", typeof<OptionalFloatConverter>)>]
        member val Floor : float Nullable = Nullable() with get, set

        [<Field(2, "EJV.X.FRNCAP", typeof<OptionalFloatConverter>)>]
        member val Cap : float Nullable = Nullable() with get, set

        [<Field(3, "EJV.X.FREQ")>]
        member val Frequency = String.Empty with get, set

        [<Field(4, "EJV.X.ADF_MARGIN", typeof<OptionalFloatConverter>)>]
        member val Margin : float Nullable = Nullable() with get, set

        [<Field(5, "EJV.X.INDEX", typeof<RequeredStringConverter>)>]
        member val IndexRic = String.Empty with get, set

        override x.ToString () = sprintf "<Cap %A / Floor %A / Freq %s / Margin %A / Index %s>" x.Cap x.Floor x.Frequency x.Margin x.IndexRic

    [<Request("RH:In;Con")>]
    type RicData() =
        [<Field(0)>]
        member val Ric = String.Empty with get, set

        [<Field(1)>]
        member val Contributor = String.Empty with get, set

        [<Field(2, "EJV.C.RICS", typeof<RequeredStringConverter>)>]
        member val ContributedRic = String.Empty with get, set