namespace polly

open System
open System.IO
open System.Threading
open NghiaBui.Common.Text
open NghiaBui.Common.Time
open MonitorCommon

module MonitorRun =

    let private sendRebootEmails senderInfo emails (error : Error)  =
        try
            Email.sendReboot senderInfo emails error.Reason error.Log
        with _ -> ()

    let private sendCrashEmails senderInfo emails =
        try
            Email.sendCrash senderInfo emails
        with _ -> ()

    let private reboot () =
        System.Diagnostics.Process.Start ("shutdown", "/r /t 1") |> ignore

    type private SimpleChannel = AsyncReplyChannel<unit>

    type private Message =
        | Start_ThenSaveStop of (unit -> unit) * (unit -> unit) * SimpleChannel
        | Stop of SimpleChannel

    let private agentForeverBody (mailbox : MailboxProcessor<Message>) =
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
        idle ()

    let forever out config =
        let agentForever = MailboxProcessor.Start agentForeverBody

        let senderInfo = Config.extractSenderInfo config

        let agentCheck = MonitorCheck.Agent (extractProfiles config, (fun error ->
            sendRebootEmails senderInfo config.Subscribes error
            reboot ()))

        let rec loop timeMs =
            let start, wait, stop =
                Process.create
                    (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "winpty.exe"))
                    (sprintf "-Xallow-non-tty -Xcolor -Xplain \"%s\" %s" config.MinerPath config.MinerArgs)
                    (fun line ->
                        out line
                        line |> cleanAnsiEscapeCode |> agentCheck.Update)

            agentForever.PostAndReply (fun channel -> Start_ThenSaveStop (start, stop, channel))

            wait ()

            sendCrashEmails senderInfo config.Subscribes

            let curTime = currentUnixTimeMs ()
            if curTime - timeMs < (int64 config.CrashToleranceMinutes) * 60000L then
                let error = { Reason = "Crash too frequently"; Log = "<No log>" }
                sendRebootEmails senderInfo config.Subscribes error
                reboot ()
            else
                out ""
                out "=================================================================="
                out "          THE MINER HAS BEEN EXITED, NOW START IT AGAIN"
                out "=================================================================="
                out ""
                agentCheck.Reset ()
                loop curTime

        let thread = Thread (ThreadStart (fun _ -> currentUnixTimeMs () |> loop))
        thread.Start ()

        let wait = fun () -> thread.Join ()
        let stop = fun () -> agentForever.PostAndReply Stop; agentCheck.Stop ()
        wait, stop
