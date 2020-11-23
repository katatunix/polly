[<AutoOpen>]
module polly.Common

open System
open System.Runtime.InteropServices
open NghiaBui.Common.Time

type TimeMs = TimeMs of int64 with
    member this.Value = let (TimeMs value) = this in value
    static member Now = currentUnixTimeMs () |> TimeMs
    static member (-) (TimeMs a, TimeMs b) =
        TimeMs (a - b)
    static member op_LessThan (TimeMs a, TimeMs b) =
        a < b
    static member op_GreaterThan (TimeMs a, TimeMs b) =
        a > b

let fromMinutes x = int64 x * 60000L |> TimeMs

let string2opt s = if String.IsNullOrEmpty s then None else Some s

type StopFun = StopFun of (unit -> unit) with
    member this.Execute () = let (StopFun f) = this in f ()

let rec waitForKey key =
    if Console.ReadKey(true).Key <> key then
        waitForKey key

type Event =
    | C = 0
    | Break = 1
    | Close = 2
    | LogOff = 5
    | Shutdown = 6

type ConsoleCtrEventHandler = delegate of int -> bool
[<DllImport("kernel32.dll")>]
extern bool SetConsoleCtrlHandler (ConsoleCtrEventHandler handler, bool add)

let registerAppExit callback =
    let handler = new ConsoleCtrEventHandler (fun ctrlType ->
        match enum ctrlType with
        | Event.C
        | Event.Close
        | Event.LogOff
        | Event.Shutdown ->
            callback ()
        | _ ->
            ()
        true
    )
    SetConsoleCtrlHandler (handler, true) |> ignore
