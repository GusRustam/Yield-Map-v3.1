namespace YieldMap.Tools.Aux

open System.Runtime.InteropServices
open System.Collections.Generic

[<AutoOpen>]
module Extensions =
    open System
    open System.Collections.Generic
    open System.IO
    open System.Reflection
    open System.Runtime.Serialization
    open System.Runtime.Serialization.Formatters.Binary
    open System.Text

    type Agent<'T> = MailboxProcessor<'T>

    let cross f a b = f b a

    // Some operators
    let inline (|-) item items = set items |> Set.contains item
    let inline (-|) items item = item |- items

    (* Расширение встроенной функциональности *)
    module Array = 
        let first (arr: 'a array) = arr.[0]
        let others (arr: 'a array) = arr.[1..]
        let split arr = first arr, others arr
        let repeat arr times = [|1..times|] |> Array.fold (fun acc _ -> Array.append acc arr) [||]
        let unique arr = arr |> Seq.distinct |> Seq.toArray

    module Map =
        let join map another = 
            let rec doInject m = function (key, value) :: rest -> doInject (Map.add key value m) rest | _ -> m
            doInject map (Map.toList another)

        let fromDict (d:Dictionary<_,_>) =
            let keys = d.Keys |> List.ofSeq
            let rec append keys (dct:Dictionary<_,_>) agg = 
                match keys with 
                | key :: rest -> 
                    let v = dct.[key]
                    append rest d (agg |> Map.add key v)
                | [] -> agg
            append keys d Map.empty

        let fromDict2 (d:Dictionary<_,Dictionary<_,_>>) =
            let keys = d.Keys |> List.ofSeq
            let rec append keys (dct:Dictionary<_,_>) agg = 
                match keys with 
                | key :: rest -> 
                    let v = dct.[key]
                    append rest d (agg |> Map.add key (fromDict v))
                | [] -> agg
            append keys d Map.empty

        let keys m = m |> Map.toList |> List.unzip |> fst |> set
        let values m = m |> Map.toList |> List.unzip |> snd |> set

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

        static member toBytes (str:string) = 
            let bytes = Array.init (str.Length * sizeof<char>) (fun _ -> 0uy)
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length)
            bytes
        
        static member fromBytes (bytes:byte array) = 
            let chars = Array.init (bytes.Length / sizeof<char>) (fun _ -> ' ')
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length)
            String(chars)

        static member toString wut = match wut with null -> null | _ -> wut.ToString()

        static member split delim (wut:string) = wut.Split(delim)

    type Async with
        static member WithTimeout (timeout:int option) operation = 
            match timeout with
            | Some(time) -> async { try return Async.RunSynchronously (operation, time) |> Some with :? TimeoutException -> return None }
            | _ -> async { return operation |> Async.RunSynchronously |> Some }

        static member WithTimeoutEx (timeout:int option) operation = 
            match timeout with
            | Some(time) -> async { return Async.RunSynchronously (operation, time) }
            | _ -> operation

[<AutoOpen>]
module Workflows =
    open System

    module AsyncAttempt = 
        type AsyncAttempt<'T> = 
        | Parallel of 'T Async
        | Func of (unit -> 'T option)
        | Bool of bool

        let always x = Func (fun () -> x)

        let runAttempt (a : AsyncAttempt<'T>) timeout =
            match a with
            | Parallel call -> Some (call |> Async.WithTimeoutEx timeout |> Async.RunSynchronously) 
            | Func call -> call()
            | Bool b -> if b then Some Unchecked.defaultof<'T> else None

        let fail = Func (fun () -> None) : AsyncAttempt<'T>
        let succeed x = always (Some x)
        let bind p rest timeout = match runAttempt p timeout with None -> fail | Some r -> (rest r)
        let delay f timeout = always (runAttempt <| f() <| timeout)
        let combine p1 p2 timeout = always (match runAttempt p1 timeout with None -> runAttempt p2 timeout | res -> res)
        let condition p guard timeout = always (match runAttempt p timeout with Some x when guard x -> Some x | _ -> None)
        let disposable (value : 'a when 'a :> IDisposable) func timeout =  always (try runAttempt (func value) timeout finally value.Dispose())

        type AsyncAttemptBuilder(timeout) = 
            member b.Bind (p, rest) = bind p rest timeout
            member b.Delay f = delay f timeout
            member b.Return x = succeed x 
            member b.ReturnFrom (x : AsyncAttempt<'T>)  = x
            member b.Combine (p1 : AsyncAttempt<'T>, p2 : AsyncAttempt<'T>) = combine p1 p2 timeout
            member b.Zero () = fail
            member b.Using (x, f) = disposable x f timeout

            [<CustomOperation("condition", MaintainsVariableSpaceUsingBind = true)>]
            member x.Condition (p, [<ProjectionParameter>] guard) = condition p guard timeout

        let imperativeTimeout timeout = AsyncAttemptBuilder timeout
        let imperative = imperativeTimeout None

//        let someAsync = async { return Some 1 }
//        let connection = async { return true }
//        let chk x = x > 4
//        let test x = if x > 5 then Some x else None
//
//        let res = imperative {
//            let a = 2 + 2
//            condition (a < 5) 
//            let! b = Parallel someAsync 
//            condition (Option.isSome <| test (a + b))
//            return 3
//        }

    module Attempt = 
        type Attempt<'T> = (unit -> 'T option)

        let runAttempt (a : Attempt<'T>) = a() 
        let fail = (fun () -> None) : Attempt<'T>
        let succeed x = (fun () -> Some(x)) : Attempt<'T>
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

            [<CustomOperation("condition", MaintainsVariableSpaceUsingBind = true)>]
            member x.Condition (p, [<ProjectionParameter>] guard) = (fun () ->
                match p() with
                | Some x when guard x -> Some x
                | _ -> None)
      
        let attempt = AttemptBuilder()

        [<RequireQualifiedAccess>]
        module AsAttempt = 
            let bool (a : bool) = fun () -> if a then Some () else None
            let value (a : 'T option) = fun () -> a
            let excn f x = try Some (f x) with _ -> None
            let optionOrExcn f x = try f x with _ -> None


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

[<AutoOpen>]
module Disposer = 
    open System
    open System.Runtime.InteropServices

    open YieldMap.Tools.Logging

    let private logger = LogFactory.create "Disposer"

    [<AbstractClass>] 
    type Disposer() = 
        abstract member DisposeUnmanaged : unit -> unit
        abstract member DisposeManaged : unit -> unit

        interface IDisposable with
            member self.Dispose() =
                logger.TraceF "Dispose()" 
                self.PerformDispose true
                GC.SuppressFinalize self

        override self.Finalize() =
            logger.TraceF "Finalize()"
            self.PerformDispose false

        member self.PerformDispose disposeManaged =
            self.DisposeUnmanaged()
            if disposeManaged then 
                self.DisposeManaged()

    type ComDisposer<'T when 'T : equality and 'T : null>(item : 'T) = 
        inherit Disposer()

        let _object = ref item
        member self.Object() = !_object
    
        override self.DisposeUnmanaged() = ()
        override self.DisposeManaged() =
            if !_object <> null && Marshal.IsComObject(!_object) then
                logger.TraceF "References left: %d" (Marshal.ReleaseComObject(_object))
                _object := null

[<RequireQualifiedAccess>]
module Ole32 = 
    open YieldMap.Tools.Logging

    let logger = LogFactory.create "Ole32"

    [<DllImport("ole32.dll")>] 
    extern void CoUninitialize()

    let killComObject (wut : 'T ref) =
        let he = !wut
        if he <> null && Marshal.IsComObject(he) then
            let refLeft = Marshal.ReleaseComObject(he) 
            logger.TraceF "References left: %d" refLeft
            wut := null