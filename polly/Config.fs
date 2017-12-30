namespace polly

open FSharp.Data
open System.IO
open Email

module Config =

    let private CONFIG_FILE = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "config.json")

    type Json = JsonProvider<"""
        {
            "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
            "MinerArgs" : "-esm 1 -gser 0",
            "Polly" : {
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

    let load () =
        try
            File.ReadAllText CONFIG_FILE
            |> Json.Parse
            |> Ok
        with ex ->
            ex.Message |> Error

    let extractSenderInfo (config : Json.Root) : SenderInfo =
        let p = config.Polly
        {   SmtpHost        = p.SmtpHost
            SmtpPort        = p.SmtpPort
            Address         = p.Address
            Password        = p.Password
            DisplayedName   = p.DisplayedName }
