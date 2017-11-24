namespace polly

open System.Diagnostics

module Monitor =

    type private Error = {
        Reason : string
        Log : string }

    let private checkLine errorIndicators (line : string) =
        errorIndicators
        |> Array.tryFind line.Contains
        |> Option.map (fun indicator -> { Reason = indicator; Log = line })

    let private sendEmail senderInfo emails (error : Error)  =
        try
            for email in emails do
                Email.sendReboot senderInfo email error.Reason error.Log
        with _ -> ()

    let private reboot () =
        Process.Start ("shutdown", "/r /t 1") |> ignore

    let start out config =
        let senderInfo = Config.extractSenderInfo config
        let checkLine = checkLine config.ErrorIndicators
        let sendEmail = sendEmail senderInfo config.SubscribedEmails

        let start, wait, stop =
            Process.run
                "winpty.exe"
                (sprintf "-Xallow-non-tty -Xplain \"%s\" %s" config.ClaymoresPath config.ClaymoresArgs)
                (fun line ->
                    out line
                    match checkLine line with
                    | None ->
                        ()
                    | Some error ->
                        sendEmail error
                        reboot ())
        start ()
        (wait, stop)
