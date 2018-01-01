namespace polly

open FSharp.Data
open System.IO

module Config =

    type Sender = {
        SmtpHost : string
        SmtpPort : int
        Address : string
        Password : string
        DisplayedName : string }

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

    type Config = {
        MinerPath : string
        MinerArgs : string
        Sender : Sender
        Subscribes : string array
        Profiles : Profile []
        StuckProfile : SpecialProfile
        QuickExitProfile : SpecialProfile
        PublicIpCheck : TimeMs
    }

    let private CONFIG_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "config.json")

    type private Json = JsonProvider<"""
        {
            "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
            "MinerArgs" : "-esm 1 -gser 0",
            "Sender" : {
                "SmtpHost" : "smtp.gmail.com",
                "SmtpPort" : 587,
                "Address" : "pollymonitor2@gmail.com",
                "Password" : "test",
                "DisplayedName" : "Polly"
            },
            "Subscribes" : [
                "apple@gmail.com",
                "banana@yahoo.com"
            ],
            "Profiles" : [
                {
                    "Bad" : [ "speed = 8" ],
                    "Tolerance" : { "DurationMinutes" : 10, "Good" : ["speed = 10"] },
                    "Action" : "restart.bat"
                },
                {
                    "Bad" : [ "got incorrect share" ],
                    "Action" : ""
                }
            ],
            "StuckProfile" : {
                "ToleranceMinutes" : 5,
                "Action" : "restart.bat"
            },
            "QuickExitProfile" : {
                "ToleranceMinutes" : 1,
                "Action" : "restart.bat"
            },
            "PublicIpCheckMinutes" : 30
        }""">

    let private parseProfiles (config : Json.Root) : Profile [] =
        config.Profiles
        |> Array.map(fun p ->
            {   Bad = p.Bad
                Tolerance = p.Tolerance
                            |> Option.map (fun t ->
                                {   Duration = t.DurationMinutes |> fromMinutes
                                    Good = t.Good })
                Action = p.Action })

    let private parseConfig (json : Json.Root) : Config =
        {
            MinerPath = json.MinerPath
            MinerArgs = json.MinerArgs
            Sender = {  SmtpHost        = json.Sender.SmtpHost
                        SmtpPort        = json.Sender.SmtpPort
                        Address         = json.Sender.Address
                        Password        = json.Sender.Password
                        DisplayedName   = json.Sender.DisplayedName }
            Subscribes = json.Subscribes
            Profiles = parseProfiles json
            StuckProfile =      {   Tolerance   = json.StuckProfile.ToleranceMinutes |> fromMinutes
                                    Action      = json.StuckProfile.Action }
            QuickExitProfile =  {   Tolerance   = json.QuickExitProfile.ToleranceMinutes |> fromMinutes
                                    Action      = json.QuickExitProfile.Action }
            PublicIpCheck = json.PublicIpCheckMinutes |> fromMinutes
        }

    let load () =
        try
            File.ReadAllText CONFIG_FILE
            |> Json.Parse
            |> parseConfig
            |> Ok
        with ex ->
            ex.Message |> Error
