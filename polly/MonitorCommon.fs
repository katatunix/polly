namespace polly

open NghiaBui.Common.Time

module MonitorCommon =

    type TimeMs = TimeMs of int64 with
        member this.Value = let (TimeMs value) = this in value
        static member (-) (TimeMs a, TimeMs b) =
            TimeMs (a - b)
        static member op_LessThan (TimeMs a, TimeMs b) =
            a < b
        static member op_GreaterThan (TimeMs a, TimeMs b) =
            a > b

    let curTime () = currentUnixTimeMs () |> TimeMs

    type FireInfo = {
        Reason : string
        UpTime : TimeMs
        Action : string option
        Log : string option }

    type Tolerance = {
        Duration : TimeMs
        Good : string [] }

    type Profile = {
        Bad : string []
        Tolerance : Tolerance option
        Action : string option }

    type SpecialProfile = {
        Tolerance : TimeMs
        Action : string }

    let minutes2TimeMs minutes = (int64 minutes) * 60000L |> TimeMs

    let extractProfiles (config : Config.Json.Root) : Profile [] =
        config.Profiles
        |> Array.map(fun p ->
            {   Bad = p.Bad
                Tolerance = p.Tolerance
                            |> Option.map (fun t ->
                                {   Duration = minutes2TimeMs t.DurationMinutes
                                    Good = t.Good })
                Action = p.Action })

    let extractStuckProfile (config : Config.Json.Root) : SpecialProfile =
        {   Tolerance = minutes2TimeMs config.StuckProfile.ToleranceMinutes
            Action = config.StuckProfile.Action }

    let extractQuickExitProfile (config : Config.Json.Root) : SpecialProfile =
        {   Tolerance = minutes2TimeMs config.QuickExitProfile.ToleranceMinutes
            Action = config.QuickExitProfile.Action }
