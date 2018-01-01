namespace polly

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
