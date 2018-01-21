namespace polly

open System
open FSharp.Data
open System.IO

module Config =

    type Sender = {
        SmtpHost : string
        SmtpPort : int
        Address : string
        Password : string
        DisplayedName : string }

    let private INDI_SEP = "___"
    let private INDI_SEPS = [| INDI_SEP |]

    type Indicator = {
        Contain : string
        NotContains : string [] } with
        member this.RawText =
            if this.NotContains.Length = 0 then
                this.Contain
            else
                this.Contain + INDI_SEP + (this.NotContains |> String.concat INDI_SEP)

    type Tolerance = {
        Duration : TimeMs
        Good : Indicator [] }

    type Profile = {
        Bad : Indicator []
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

    let private DEFAULT_CONFIG_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "config.json")

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
                    "Bad" : [ "ETH - Total Speed:___ETH - Total Speed: 18" ],
                    "Tolerance" : { "DurationMinutes" : 10, "Good" : [ "ETH - Total Speed: 18" ] },
                    "Action" : "restart.bat"
                },
                {
                    "Bad" : [ "fan=0%" ],
                    "Tolerance" : { "DurationMinutes" : 10, "Good" : [ "fan=___fan=0%" ] },
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

    let private parseIndicator (indicator : string) =
        let keys = indicator.Split (INDI_SEPS, StringSplitOptions.RemoveEmptyEntries)
        if keys.Length = 0 then
            { Contain = ""; NotContains = [||] }
        else
            { Contain = keys.[0]; NotContains = keys |> Array.tail }

    let private parseProfiles (config : Json.Root) : Profile [] =
        config.Profiles
        |> Array.map(fun p ->
            {   Bad = p.Bad |> Array.map parseIndicator
                Tolerance = p.Tolerance
                            |> Option.map (fun t ->
                                {   Duration = t.DurationMinutes |> fromMinutes
                                    Good = t.Good |> Array.map parseIndicator })
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

    let load configFile =
        try
            configFile
            |> Option.defaultValue DEFAULT_CONFIG_FILE
            |> File.ReadAllText
            |> Json.Parse
            |> parseConfig
            |> Ok
        with ex ->
            ex.Message |> Error
