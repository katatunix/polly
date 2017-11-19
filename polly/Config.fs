namespace polly

open FSharp.Data
open System.IO

module Config =

    type Json = JsonProvider<"""
        {
            "Port" : 3333,
            "CheckIntervalMinutes" : 7,
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
            