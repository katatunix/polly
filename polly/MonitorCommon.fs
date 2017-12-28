namespace polly

module MonitorCommon =

    type TimeMs = TimeMs of int64 with
        static member (-) (TimeMs a, TimeMs b) =
            TimeMs (a - b)
        static member op_LessThan (TimeMs a, TimeMs b) =
            a < b
        static member op_GreaterThan (TimeMs a, TimeMs b) =
            a > b

    type Error = {
        Reason : string
        UpTime : TimeMs
        Log : string }

    type Tolerance = {
        Duration : TimeMs
        Good : string [] }

    type Profile = {
        Bad : string []
        Tolerance : Tolerance option }

    let extractProfiles (config : Config.Json.Root) : Profile [] =
        config.Profiles
        |> Array.map(fun p ->
            {   Bad = p.Bad
                Tolerance = p.Tolerance
                            |> Option.map (fun t ->
                                {   Duration = TimeMs ((int64 t.DurationMinutes) * 60000L)
                                    Good = t.Good }) })
