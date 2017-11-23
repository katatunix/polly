namespace polly

open System
open System.Diagnostics
open System.Timers

module Main =

    let sendPublicIpToEmails senderInfo ip emails =
        try
            for email in emails do
                printf "Send public IP address to %s ... " email
                Email.sendPublicIp senderInfo email ip
                printfn "[OK]"
        with _ -> ()

    let sendResetToEmails senderInfo (error : Miner.Error) emails =
        try
            for email in emails do
                 Email.sendReset senderInfo email error.Reason error.Log
        with _ -> ()

    let resetComputer () =
        Process.Start ("shutdown", "/r /t 1") |> ignore

    let extractSenderInfo (config : Config.Json.Root) : Email.SenderInfo =
        let p = config.Polly
        {   SmtpHost        = p.SmtpHost
            SmtpPort        = p.SmtpPort
            Email           = p.Email
            Password        = p.Password
            DisplayedName   = p.DisplayedName }

    let checkIp senderInfo (config : Config.Json.Root) =
        match PublicIp.get () with
        | Error msg ->
            printfn "Could not get public IP: %s" msg
        | Ok ip ->
            printfn "Public IP = %s" ip
            if ip <> PublicIp.load () then
                sendPublicIpToEmails senderInfo ip config.SubscribedEmails
                PublicIp.save ip

    let start (config : Config.Json.Root) =
        let senderInfo = extractSenderInfo config
        let checkIp () = checkIp senderInfo config

        checkIp ()

        use timer = new Timer (config.CheckIntervalMinutes * 60 * 1000 |> float)
        timer.Elapsed.Add (fun _ ->
            match Miner.check config.Port config.ErrorIndicators with
            | None ->
                printfn "[%A] OK" DateTime.Now
                checkIp ()
            | Some error ->
                sendResetToEmails senderInfo error config.SubscribedEmails
                resetComputer ())

        timer.Start ()

        let rec loop () =
            let key = Console.ReadKey(true).Key
            if key <> ConsoleKey.Escape then loop ()
        loop ()

        timer.Stop ()

    [<EntryPoint>]
    let main argv =
        match Config.load () with
        | Error msg ->
            printfn "Error: %s" msg
            1
        | Ok config ->
            start config
            0
