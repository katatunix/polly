namespace NghiaBui

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Runtime.Serialization.Formatters.Binary

module Common =

    /// IHS = Immutable Hash Set
    type IHS<'T> when 'T:equality (core : HashSet<'T>) =
        new (s : seq<'T>) = IHS (HashSet s)
        new () = IHS (HashSet ())

        member this.Contains x = core.Contains x
        member this.Count = core.Count
        member this.IsEmpty = core.Count = 0

        static member (+) (a : IHS<'T>, b : IHS<'T>) =
            let c = HashSet a
            c.UnionWith b
            IHS c

        static member (-) (a : IHS<'T>, b : IHS<'T>) =
            let c = HashSet a
            for x in b do c.Remove x |> ignore
            IHS c

        static member (==) (a : IHS<'T>, b : IHS<'T>) =
            a.Count = b.Count && (a - b).IsEmpty

        interface IEnumerable<'T> with
            member this.GetEnumerator () =
                (core :> IEnumerable<'T>).GetEnumerator ()
        interface IEnumerable with
            member this.GetEnumerator () =
                (core :> IEnumerable).GetEnumerator ()

    module IHS =
        let filter pred set =
            let res = HashSet ()
            for x in set do
                if pred x then res.Add x |> ignore
            IHS res

        let map mapper set =
            let res = HashSet ()
            for x in set do
                res.Add (mapper x) |> ignore
            IHS res

    let (===) x (xs : IHS<'T>) = xs.Contains x
    let (!===) x xs = not (x === xs)

    let log2 = let a = log 2.0 in fun x -> (log x) / a

    let isNotNull x = not (isNull x)

    let failShouldNotGoHere () = failwith "Should not go here"

    let partition pred (set : IHS<'T>) =
        let set1 = HashSet ()
        let set2 = HashSet ()
        for e in set do
            if pred e then set1.Add e |> ignore
            else set2.Add e |> ignore
        IHS set1, IHS set2

    let foldWithEarlyExit exitCondition folder (state : 'State) (source : seq<'T>) =
        let iter = source.GetEnumerator()
        let rec loop state =
            if iter.MoveNext() = false || exitCondition state then state
            else
                folder state iter.Current |> loop
        loop state

    let getKeys map = map |> Map.toSeq |> Seq.map fst

    let getKeySet map = map |> getKeys |> Set.ofSeq

    let countIntersect (setA : IHS<'T>) (setB : IHS<'T>) =
        setA |> Seq.fold (fun count a -> if a === setB then count + 1 else count) 0

    let dictFromSeq s =
        let d = Dictionary ()
        for (k, v) in s do
            d.Add (k, v)
        d

    let findMax lessFun xs =
        let mutable result = None
        for x in xs do
            match result with
            | None ->
                result <- Some x
            | Some currentMax ->
                if lessFun currentMax x then result <- Some x
        result

    let memoize f =
        let cache = Dictionary ()
        fun a ->
            if cache.ContainsKey a then
                cache.[a]
            else
                let b = f a in cache.Add (a, b); b
        
    type StopWatch () =
        let mutable last = DateTime.Now
        member this.ElapseMs =
            int (DateTime.Now - last).TotalMilliseconds
        member this.ElapseSec =
            this.ElapseMs / 1000
        member this.Reset () =
            last <- DateTime.Now

    let (|Int|_|) str =
       match System.Int32.TryParse str with
       | true, x -> Some x
       | _ -> None

    let (|Float|_|) str =
       match System.Double.TryParse str with
       | true, x -> Some x
       | _ -> None

    let (|Array|_|) arr =
        if arr |> Array.length = 0 then None
        else Some (arr.[0], arr |> Array.tail)

    module Rop =
        let liftBool f error =
            fun x -> if f x then Ok () else Error error
        
        let liftOpt f error =
            fun x -> match f x with
                        | Some y -> Ok y
                        | None -> Error error
        
        let liftExn f =
            fun x -> try f x |> Ok with ex -> ex.Message |> Error

        type Builder () =
            member this.Bind (m, f) =
                m |> Result.bind f
            member this.Return x =
                Ok x
            member this.ReturnFrom x =
                x
            member this.Zero () =
                Ok ()

        let rop = Builder ()

    module Maybe =
        type Builder () =
            member this.Bind (m, f) =
                m |> Option.bind f
            member this.Return x =
                Some x
            member this.ReturnFrom x =
                x
            member this.Zero () =
                Some ()
        let maybe = Builder ()

    module IO =
        let writeValue stream x =
            let formatter = BinaryFormatter ()
            formatter.Serialize (stream, box x)
        
        let readValue stream =
            let formatter = BinaryFormatter ()
            let res = formatter.Deserialize stream
            unbox res

        let writeValueToFile x path =
            use stream = new FileStream (path, FileMode.Create)
            writeValue stream x

        let readValueFromFile path =
            use stream = new FileStream (path, FileMode.Open)
            readValue stream

    [<AllowNullLiteral>]
    type WebPath (homeUrl, homeDisk) =
        let trimSlash (str : string) =
            if str.EndsWith "/" then
                str.Substring (0, str.Length - 1)
            else
                str
        let homeUrl = trimSlash homeUrl
        let homeDisk = trimSlash homeDisk

        member x.Home = homeUrl
        member x.HomeWithSlash = homeUrl + "/"

        member x.ConvertToDisk (url : string) =
            if url.StartsWith x.HomeWithSlash then
                Some (System.IO.Path.Combine (homeDisk, url.Substring homeUrl.Length))
            else
                None
