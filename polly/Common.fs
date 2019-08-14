namespace polly

open System
open System.Runtime.InteropServices
open NghiaBui.Common.Time

[<AutoOpen>]
module Common =

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

    let CTRL_C_EVENT = 0
    let CTRL_BREAK_EVENT = 1
    let CTRL_CLOSE_EVENT = 2
    let CTRL_LOGOFF_EVENT = 5
    let CTRL_SHUTDOWN_EVENT = 6
    type ConsoleCtrEventHandler = delegate of int -> bool
    [<DllImport("kernel32.dll")>]
    extern bool SetConsoleCtrlHandler (ConsoleCtrEventHandler handler, bool add)

    let registerAppExit callback =
        let handler = new ConsoleCtrEventHandler (fun ctrlType ->
            if  ctrlType = CTRL_C_EVENT ||
                ctrlType = CTRL_CLOSE_EVENT ||
                ctrlType = CTRL_LOGOFF_EVENT ||
                ctrlType = CTRL_SHUTDOWN_EVENT then
                callback ()
            true)
        SetConsoleCtrlHandler (handler, true) |> ignore
