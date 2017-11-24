namespace polly

open FSharp.Data
open System.IO
open Email

module Config =

    type Json = JsonProvider<"""
        {
            "MinerPath" : "D:/Claymores/EthDcrMiner64.exe",
            "MinerArgs" : "-esm 1 -gser 0",
            "Polly" : {
                "SmtpHost" : "smtp.gmail.com",
                "SmtpPort" : 587,
                "Email" : "pollymonitor2@gmail.com",
                "Password" : "test",
                "DisplayedName" : "Polly"
            },
            "SubscribedEmails" : [
                "apple@gmail.com",
                "banana@yahoo.com"
            ],
            "ErrorIndicators" : [
                "fan=0%",
                "got incorrect share"
            ]
        }""">

    let load () =
        try
            File.ReadAllText("config.json")
            |> Json.Parse
            |> Ok
        with ex ->
            ex.Message |> Error

    let extractSenderInfo (config : Json.Root) : SenderInfo =
        let p = config.Polly
        {   SmtpHost        = p.SmtpHost
            SmtpPort        = p.SmtpPort
            Email           = p.Email
            Password        = p.Password
            DisplayedName   = p.DisplayedName }
