namespace polly

open System.Threading
open NghiaBui.Common.Text

module Monitor =

    type private Error = {
        Reason : string
        Log : string }

    let private checkLine errorIndicators (line : string) =
        errorIndicators
        |> Array.tryFind line.Contains
        |> Option.map (fun indicator -> { Reason = indicator; Log = line })

    let private sendRebootEmail senderInfo emails (error : Error)  =
        try
            Email.sendReboot senderInfo emails error.Reason error.Log
        with _ -> ()

    let private sendCrashEmail senderInfo emails =
        try
            Email.sendCrash senderInfo emails
        with _ -> ()

    let private reboot () =
        System.Diagnostics.Process.Start ("shutdown", "/r /t 1") |> ignore

    let private prepare out config =
        let senderInfo = Config.extractSenderInfo config
        let checkLine = checkLine config.ErrorIndicators
        let sendRebootEmail = sendRebootEmail senderInfo config.SubscribedEmails

        Process.create
            "winpty.exe"
            (sprintf "-Xallow-non-tty -Xcolor -Xplain \"%s\" %s" config.MinerPath config.MinerArgs)
            (fun line ->
                out line
                match line |> cleanAnsiEscapeCode |> checkLine with
                | None ->
                    ()
                | Some error ->
                    sendRebootEmail error
                    reboot ())

    let startOnce out config =
        let start, wait, stop = prepare out config
        start ()
        wait, stop

    type private SimpleChannel = AsyncReplyChannel<unit>

    type private Message =
        | Start_ThenSaveStop of (unit -> unit) * (unit -> unit) * SimpleChannel
        | Stop of SimpleChannel

    let startForever out config =
        let mailbox = MailboxProcessor.Start (fun mailbox ->
            let rec handle stopOp =
                async {
                    let! message = mailbox.Receive ()
                    match message with
                    | Start_ThenSaveStop (start, stop, channel) ->
                        start ()
                        channel.Reply ()
                        return! active stop
                    | Stop channel ->
                        stopOp |> Option.iter (fun stop -> stop ())
                        channel.Reply ()
                        return () }
            and idle () = handle None
            and active stop = handle (Some stop)
            idle ())

        let stop = fun () -> mailbox.PostAndReply Stop

        let senderInfo = Config.extractSenderInfo config
        let sendCrashEmail () = sendCrashEmail senderInfo config.SubscribedEmails

        let rec loop () =
            let start, wait, stop = prepare out config
            mailbox.PostAndReply (fun channel -> Start_ThenSaveStop (start, stop, channel))
            wait ()

            out ""
            out "=================================================================="
            out "          THE MINER HAS BEEN EXITED, NOW START IT AGAIN"
            out "=================================================================="
            out ""

            sendCrashEmail ()

            loop ()

        let thread = Thread (ThreadStart loop)
        thread.Start ()
        let wait = fun () -> thread.Join ()

        wait, stop
