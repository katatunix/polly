namespace polly

module MonitorCommon =

    type TimeMs = TimeMs of int64

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
